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
    // 네이티브·MSAA 캐럿은 편집 필드에서만 존재 → 편집 확정. UIA 캐럿은 selection 기반이라
    // 읽기 전용 텍스트(브라우저 본문 클릭)에서도 잡힘 → 편집 확정 근거로 안 쓴다.
    private static readonly CaretResolver Caret = new(
        new CaretResolver.Source(GuiThreadInfoCaret.TryGet, ImpliesEditable: true),
        new CaretResolver.Source(UiaCaret.TryGet, ImpliesEditable: false),
        new CaretResolver.Source(MsaaCaret.TryGet, ImpliesEditable: true));

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

    /// <summary>인디케이터(캐럿 옆 원) 배경색. 사용자 설정.</summary>
    public Color IndicatorColor
    {
        set => _overlay.SetColor(value);
    }

    /// <summary>인디케이터 글자색. 사용자 설정.</summary>
    public Color IndicatorTextColor
    {
        set => _overlay.SetTextColor(value);
    }

    /// <summary>현재 입력 상태 글자(한/영/あ 등)를 알린다. 트레이 아이콘 갱신용.</summary>
    public Action<string>? StateChanged { get; set; }

    private IntPtr _lastForeground;              // 포그라운드 창 전환 감지용(전역 포커스 이벤트 잡음 무시)
    private long _lastForegroundChangeTick = Environment.TickCount64;

    // 편집 포커스 판정 캐시. 편집 가능 여부는 포커스된 요소의 성질이지 매 틱의 사실이 아니다 —
    // 그런데 판정에는 UIA(크로스 프로세스)가 필요해 매 틱 물으면 대상 앱 UI 스레드를 갉아먹는다.
    // 그래서 포커스가 바뀔 때만 다시 묻는다(WinEvent 포커스 이벤트 + hwndFocus 변경으로 무효화).
    private bool? _editableFocus;
    private IntPtr _editableFocusKey;

    // 표시 중에 쓰는 캐럿 위치와, 그걸 해석한 시점의 마지막 입력 도장.
    // 도장이 그대로면 입력이 없었다는 뜻이고, 캐럿은 입력으로만 움직이므로 다시 읽지 않는다.
    private Rectangle? _shownCaret;
    private uint? _shownCaretInputTick;

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
        //
        // 편집 포커스 캐시는 포그라운드 스레드의 이벤트일 때만 버린다. Chromium 계열은
        // DOM 포커스가 바뀌어도 hwndFocus가 그대로라 이 이벤트가 유일한 무효화 신호지만,
        // 배경 앱 이벤트로까지 버리면 캐시가 의미를 잃는다.
        if (thread == Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out _))
            _editableFocus = null;

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
            // 편집 필드를 벗어나 유휴로 들어가는 시점 — 작업 집합을 반납해 RAM 점유를 낮춘다.
            Win32.TrimWorkingSet();
        }
    }

    private InputSnapshot ReadSnapshot(Size indicatorSize)
    {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            _lastForeground = IntPtr.Zero;
            _editableFocus = null;
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
        if (foreground != _lastForeground)
        {
            _lastForeground = foreground;
            _lastForegroundChangeTick = Environment.TickCount64;
            _editableFocus = null;
        }

        uint threadId = Win32.GetWindowThreadProcessId(foreground, out _);
        var (langId, conversionMode) = ImeStateReader.Read(foreground, threadId);

        // --- 여기까지가 매 틱 도는 값싼 구간(전부 로컬 호출) ---
        bool editable = IsEditableFocus(threadId);
        uint lastInputTick = InputActivity.LastInputTick();
        long millisSinceActivity = Decider.MillisSinceActivity(
            InputActivity.MillisSince(lastInputTick),
            Environment.TickCount64 - _lastForegroundChangeTick);

        // --- 비싼 구간: 캐럿 해석(UIA/MSAA) ---
        // 타이핑 중에는 어차피 숨김이라 위치가 필요 없고, 표시 중이라도 캐럿은 입력이 있을
        // 때만 움직인다. 그래서 (a) 표시할 상황이고 (b) 지난 해석 이후 새 입력이 있었을 때만
        // 다시 읽는다. 이 게이트가 없으면 매 틱 대상 앱(Chromium 계열이면 특히 비싸다)의
        // UI 스레드를 동기로 붙잡아 입력이 씹힌다.
        // ("항상 표시" 설정(0초)에서는 입력마다 도장이 바뀌므로 캐럿을 계속 따라간다.)
        if (!Decider.ShouldShow(editable, millisSinceActivity, _idleReappearMs))
        {
            _shownCaret = null;
            _shownCaretInputTick = null;
        }
        else if (_shownCaretInputTick != lastInputTick)
        {
            _shownCaret = Caret.Resolve(threadId)?.Rect;
            _shownCaretInputTick = lastInputTick;
        }
        Rectangle? caret = _shownCaret;

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

    // 편집 포커스 여부. 판정 자체는 예전과 같지만(편집 확정 캐럿이면 인정, 아니면 UIA로 확인)
    // 매 틱이 아니라 포커스가 바뀔 때만 다시 묻고 그 사이엔 캐시를 쓴다.
    // 네이티브 캐럿(GetGUIThreadInfo)은 로컬 호출이라 공짜이므로 캐시 없이 매 틱 확인해,
    // 같은 hwndFocus 안에서 캐럿이 생겼다 없어지는 변화는 즉시 반영된다.
    private bool IsEditableFocus(uint threadId)
    {
        if (GuiThreadInfoCaret.TryGet(threadId) is not null)
            return true;

        IntPtr focusHwnd = Win32.TryGetGuiThreadInfo(threadId, out var gti) ? gti.hwndFocus : IntPtr.Zero;
        if (_editableFocus is bool cached && focusHwnd == _editableFocusKey)
            return cached;

        // 캐시 미스 — 여기서만 UIA/MSAA를 문다.
        var caretHit = Caret.Resolve(threadId);
        bool editable = caretHit?.ImpliesEditable == true || FocusInspector.IsTextControl();

        // 긍정만 캐시한다. 부정까지 캐시하면 갇힌다 — 편집 포커스가 아니면 폴링 타이머가
        // 멈추고, 그 뒤엔 캐시를 깰 WinEvent(포그라운드 스레드의 포커스 변경)가 오지 않는 한
        // 다시 평가되지 않는다. 같은 입력창에 계속 타이핑하는 동안엔 그 이벤트가 안 오므로
        // 인디케이터가 영영 안 뜬다. 부정일 때의 회복 경로는 예전과 같이 매번 재평가로 둔다.
        if (editable)
        {
            _editableFocusKey = focusHwnd;
            _editableFocus = true;
        }
        else
        {
            _editableFocus = null;
        }

        return editable;
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
