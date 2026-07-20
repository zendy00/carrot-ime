using System.Drawing;
using System.Windows.Forms;
using ImeCaretIndicator.Adapters;
using ImeCaretIndicator.Core;
using ImeCaretIndicator.Ui;

namespace ImeCaretIndicator;

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
        => Update();

    private void Update()
    {
        InputSnapshot snapshot = ReadSnapshot(_overlay.PreferredIndicatorSize);
        IndicatorView view = Decider.Decide(snapshot);

        if (view.Visible)
        {
            _overlay.Render(view.Label, view.Position);
            if (!_inputStateTimer.Enabled)
                _inputStateTimer.Start(); // 텍스트 필드 있는 동안만 한/영 폴링
        }
        else
        {
            _overlay.HideIndicator();
            if (_inputStateTimer.Enabled)
                _inputStateTimer.Stop(); // 유휴 → 폴링 정지 (CPU ~0)
        }
    }

    private static InputSnapshot ReadSnapshot(Size indicatorSize)
    {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return new InputSnapshot(false, null, Rectangle.Empty, Rectangle.Empty, indicatorSize, 0, 0);

        uint threadId = Win32.GetWindowThreadProcessId(foreground, out _);
        var (langId, conversionMode) = ImeStateReader.Read(foreground, threadId);
        Rectangle? caret = Caret.Resolve(threadId);

        // 캐럿을 얻었으면 편집 포커스 확정. 못 얻었을 때만 UIA로 텍스트 컨트롤 여부 추가 확인.
        bool editable = caret is not null || FocusInspector.IsTextControl();

        Rectangle activeWindow = GetWindowBounds(foreground);
        // 캐럿이 있으면 그 화면, 없으면(폴백) 활성 창이 놓인 화면 기준.
        Point anchor = caret?.Location ?? new Point(activeWindow.Left, activeWindow.Top);
        Rectangle screen = Screen.FromPoint(anchor).Bounds;

        return new InputSnapshot(
            editable, caret, activeWindow, screen, indicatorSize, langId, conversionMode);
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
