using System.Drawing;
using System.Windows.Forms;
using CarrotIME.Adapters;
using CarrotIME.Core;
using CarrotIME.Ui;

namespace CarrotIME;

/// <summary>
/// 명령형 셸: WinEvent(포그라운드/포커스)로 깨어나 스냅샷을 읽고 → Decide → 오버레이에 적용.
/// 한/영 토글은 이벤트가 없으므로, 텍스트 필드에 있는 동안(표시 중)에만 가벼운 타이머로 폴링한다.
/// 유휴 시(편집 포커스 없음)에는 타이머를 멈춰 CPU 사용을 0에 수렴시킨다.
/// </summary>
internal sealed class IndicatorController : IDisposable
{
    private static readonly CaretResolver Caret = new(
        GuiThreadInfoCaret.TryGet,
        UiaCaret.TryGet,
        MsaaCaret.TryGet);

    private readonly OverlayForm _overlay = new();
    private readonly System.Windows.Forms.Timer _inputStateTimer;
    private readonly Win32.WinEventProc _winEventProc; // GC 방지: 필드로 참조 유지

    private IntPtr _foregroundHook;
    private IntPtr _focusHook;

    private bool _enabled = true;
    private long _idleReappearMs = Decider.DefaultIdleReappearMs;

    /// <summary>일시정지 토글. 앱 수명주기(입력 사실이 아님)라 셸에서 게이트한다.</summary>
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; Update(); }
    }

    /// <summary>입력 후 다시 표시되기까지의 유휴 시간(초). 사용자 설정.</summary>
    public int IdleSeconds
    {
        set { _idleReappearMs = value * 1000L; Update(); }
    }

    /// <summary>현재 입력 상태 글자(한/영/あ 등)를 알린다. 트레이 아이콘 갱신용.</summary>
    public Action<string>? StateChanged { get; set; }

    // 입력 활동(캐럿 이동) 추적 — debounce 표시용.
    private Rectangle? _lastCaret;
    private long? _lastActivityTick; // null = 활동 기록 없음(유휴로 간주). TickCount64==0 충돌 방지.
    private IntPtr _lastForeground;   // 포그라운드 창 전환 감지용(전역 포커스 이벤트 잡음 무시)

    public IndicatorController()
    {
        _ = _overlay.Handle; // 표시 전 핸들 생성
        _inputStateTimer = new System.Windows.Forms.Timer { Interval = 120 };
        _inputStateTimer.Tick += (_, _) => Update();
        _winEventProc = OnWinEvent;
    }

    public void Start()
    {
        _foregroundHook = Hook(Win32.EVENT_SYSTEM_FOREGROUND);
        _focusHook = Hook(Win32.EVENT_OBJECT_FOCUS);
        Update();
    }

    private IntPtr Hook(uint winEvent) => Win32.SetWinEventHook(
        winEvent, winEvent, IntPtr.Zero, _winEventProc, 0, 0, Win32.WINEVENT_OUTOFCONTEXT);

    // OUTOFCONTEXT 콜백은 이 스레드의 메시지 루프에서 호출됨 → 오버레이 직접 조작 안전
    private void OnWinEvent(
        IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        // 전역 이벤트라 배경 앱 것도 들어온다 → 여기선 재평가만 하고, 유휴 리셋 여부는
        // ReadSnapshot에서 '실제 포그라운드 창이 바뀌었는지'로 판단한다.
        Update();
    }

    private void Update()
    {
        // 일시정지 상태면 아무것도 표시하지 않고 폴링도 멈춘다(앱 on/off는 셸 관심사).
        if (!_enabled)
        {
            _overlay.HideIndicator();
            if (_inputStateTimer.Enabled)
                _inputStateTimer.Stop();
            return;
        }

        InputSnapshot snapshot = ReadSnapshot(_overlay.PreferredIndicatorSize);
        IndicatorView view = Decider.Decide(snapshot, _idleReappearMs);

        if (view.Visible)
            _overlay.Render(view.Label, view.Position);
        else
            _overlay.HideIndicator();

        // 트레이 아이콘은 debounce와 무관하게 현재 입력 상태 글자(가/A/あ 등)를 반영한다.
        // (langId==0은 포그라운드 창이 없는 경우 → 갱신 생략)
        if (snapshot.KeyboardLangId != 0)
        {
            var state = Decider.ResolveState(snapshot.KeyboardLangId, snapshot.ConversionMode);
            StateChanged?.Invoke(Decider.TrayLabel(state, snapshot.KeyboardLangId));
        }

        // 편집 필드에 있는 동안엔(입력 중 숨김 상태여도) 폴링 유지 — 입력 활동/약 20초 유휴를
        // 감지해야 다시 표시할 수 있음. 필드를 벗어나면 폴링 정지 → 유휴 CPU ~0.
        if (snapshot.EditableFocus)
        {
            if (!_inputStateTimer.Enabled)
                _inputStateTimer.Start();
        }
        else if (_inputStateTimer.Enabled)
        {
            _inputStateTimer.Stop();
        }
    }

    private InputSnapshot ReadSnapshot(Size indicatorSize)
    {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            _lastCaret = null;
            _lastForeground = IntPtr.Zero;
            return new InputSnapshot(
                EditableFocus: false,
                Caret: null,
                ActiveWindowBounds: Rectangle.Empty,
                ScreenBounds: Rectangle.Empty,
                IndicatorSize: indicatorSize,
                KeyboardLangId: 0,
                ConversionMode: 0,
                MillisSinceInputActivity: long.MaxValue);
        }

        // 실제 포그라운드 창이 바뀐 경우에만 유휴 카운트를 리셋한다(배경 앱의 전역 포커스
        // 이벤트로는 리셋하지 않음 — 그게 20초 유휴를 계속 깨서 인디케이터가 안 뜨던 원인).
        bool foregroundChanged = foreground != _lastForeground;
        _lastForeground = foreground;

        uint threadId = Win32.GetWindowThreadProcessId(foreground, out _);
        var (langId, conversionMode) = ImeStateReader.Read(foreground, threadId);
        Rectangle? caret = Caret.Resolve(threadId);

        // 캐럿을 얻었으면 편집 포커스 확정. 못 얻었을 때만 UIA로 텍스트 컨트롤 여부 추가 확인.
        bool editable = caret is not null || FocusInspector.IsTextControl();

        long millisSinceActivity = TrackActivity(caret, foregroundChanged);

        Rectangle activeWindow = GetWindowBounds(foreground);
        // 캐럿이 있으면 그 지점, 없으면(폴백) 배지가 놓일 활성 창 우상단이 속한 화면 기준.
        // (우상단 기준으로 골라야 멀티모니터에서 배지가 엉뚱한 화면으로 클램프되지 않음)
        Point anchor = caret?.Location ?? new Point(activeWindow.Right, activeWindow.Top);
        Rectangle screen = Screen.FromPoint(anchor).Bounds;

        return new InputSnapshot(
            EditableFocus: editable,
            Caret: caret,
            ActiveWindowBounds: activeWindow,
            ScreenBounds: screen,
            IndicatorSize: indicatorSize,
            KeyboardLangId: langId,
            ConversionMode: conversionMode,
            MillisSinceInputActivity: millisSinceActivity);
    }

    // 캐럿 이동을 입력 활동으로 보고, 마지막 활동 이후 경과 ms를 돌려준다.
    // 창 전환 시점을 활동 기준으로 삼는다 → 창을 바꾸면 그때부터, 약 20초 유휴해야 표시.
    // 주의: 캐럿을 못 얻는 앱(크롬 폴백)에선 이동을 감지할 수 없어, 유휴 임계 후 표시되면
    //       입력을 시작해도 숨겨지지 않는다 — 활동을 알 방법이 없으므로 의도된 한계.
    private long TrackActivity(Rectangle? caret, bool foregroundChanged)
    {
        long now = Environment.TickCount64;

        if (foregroundChanged)
        {
            _lastCaret = caret;          // 기준선만 갱신
            _lastActivityTick = now;     // 창 전환 시점부터 유휴 카운트 시작(약 20초 지나야 표시)
        }
        else if (caret is Rectangle c && _lastCaret is Rectangle last)
        {
            if (c != last)
                _lastActivityTick = now; // 캐럿이 움직임 = 입력 중
            _lastCaret = caret;
        }
        else if (caret is not null)
        {
            _lastCaret = caret; // 이전에 캐럿이 없던 상태에서 기준선 확립
        }

        return _lastActivityTick is long tick ? now - tick : long.MaxValue;
    }

    private static Rectangle GetWindowBounds(IntPtr hwnd)
    {
        if (Win32.GetWindowRect(hwnd, out var r))
            return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        return Rectangle.Empty;
    }

    public void Dispose()
    {
        if (_foregroundHook != IntPtr.Zero)
            Win32.UnhookWinEvent(_foregroundHook);
        if (_focusHook != IntPtr.Zero)
            Win32.UnhookWinEvent(_focusHook);
        _inputStateTimer.Dispose();
        _overlay.Dispose();
    }
}
