using System.Drawing;

namespace CarrotIME.Core;

/// <summary>
/// 유일한 seam. 모든 결정 로직(상태 판별·라벨·위치·표시 여부)이 이 순수 함수 뒤에 모인다.
/// OS 접근은 전혀 하지 않는다 — 오직 InputSnapshot 값만 보고 IndicatorView 값을 낸다.
/// </summary>
public static class Decider
{
    // Win32 IME 조합 모드 플래그: 한글(네이티브) 입력 여부.
    private const uint ImeCmodeNative = 0x0001;

    // PRIMARYLANGID 마스크와 IME 언어들.
    private const ushort PrimaryLangMask = 0x03ff;
    private const ushort LangKorean = 0x0012;
    private const ushort LangJapanese = 0x0011;
    private const ushort LangChinese = 0x0004;

    // 캐럿과 인디케이터 사이 간격 — 방금 친 글자를 가리지 않도록.
    private const int CaretGapX = 4;

    // 캐럿(세로 막대·문자 폭 확장)으로 볼 수 있는 최대 폭. 이보다 넓으면 실제 캐럿이 아니라
    // 입력 박스·선택 영역 rect다(빈 웹 입력필드에서 UIA selection이 박스 전체를 주는 경우).
    private const int MaxCaretWidth = 20;

    // 고정 폴백 위치(활성 창 우상단)의 안쪽 여백.
    private const int FallbackMargin = 6;

    // 입력 중에는 숨기고, 이만큼 유휴하면 다시 표시(ms). 사용자가 설정으로 바꿀 수 있고,
    // 지정이 없으면 이 기본값(약 20초)을 쓴다.
    public const long DefaultIdleReappearMs = 20_000;

    /// <summary>키보드 레이아웃 언어 ID와 IME 조합 모드로 입력 상태를 판별한다.</summary>
    public static InputState ResolveState(ushort keyboardLangId, uint conversionMode)
    {
        ushort primary = (ushort)(keyboardLangId & PrimaryLangMask);
        switch (primary)
        {
            case LangKorean:
                // 한글 조합 모드면 한글, 아니면(영문 모드) 영문.
                return (conversionMode & ImeCmodeNative) != 0 ? InputState.Hangul : InputState.English;
            case LangJapanese:
            case LangChinese:
                return InputState.OtherIme; // 일본어·중국어 등 다른 입력기 (Ticket 05)
            default:
                return InputState.English; // 그 외 라틴 레이아웃은 영문 취급
        }
    }

    /// <summary>입력 상태에 대응하는 오버레이(캐럿 옆) 라벨. 한글=한, 영문=A. OtherIme는 언어별 글자.</summary>
    public static string Label(InputState state, ushort keyboardLangId = 0) => state switch
    {
        InputState.Hangul => "한",
        InputState.English => "A",
        InputState.OtherIme => OtherImeLabel(keyboardLangId),
        _ => "IME"
    };

    // 기타 IME는 언어 대표 글자로, 못 알아내면 "IME"로 폴백.
    private static string OtherImeLabel(ushort keyboardLangId) => (keyboardLangId & PrimaryLangMask) switch
    {
        LangJapanese => "あ",
        LangChinese => "中",
        _ => "IME"
    };

    /// <summary>
    /// 트레이 아이콘용 라벨. 오버레이(한/영)와 달리 한글=가, 영문=A로 표시한다.
    /// 기타 IME는 오버레이와 동일한 언어 글자.
    /// </summary>
    public static string TrayLabel(InputState state, ushort keyboardLangId = 0) => state switch
    {
        InputState.Hangul => "가",
        InputState.English => "A",
        InputState.OtherIme => OtherImeLabel(keyboardLangId),
        _ => "IME"
    };

    /// <summary>
    /// 인디케이터를 표시할 상황인지만 판정한다 — 캐럿 위치는 보지 않는다.
    /// 셸이 <b>캐럿을 읽기 전에</b> 이걸 먼저 물어, 어차피 숨길 상황이면 비싼 캐럿 해석
    /// (UIA는 포커스된 앱의 UI 스레드를 동기로 붙잡아 타이핑을 지연시킨다)을 건너뛴다.
    /// <see cref="Decide"/>의 표시 여부와 항상 같은 답을 준다.
    /// </summary>
    public static bool ShouldShow(bool editableFocus, long millisSinceInputActivity, long idleReappearMs)
        // Ticket 03: 편집 가능한 텍스트 포커스가 있을 때만 표시.
        // 입력 중에는 방해하지 않도록 숨기고, 설정된 유휴 시간이 지나면 다시 표시.
        => editableFocus && millisSinceInputActivity >= idleReappearMs;

    /// <summary>
    /// 입력 활동 기준 경과 시간. 마지막 키/마우스 입력과 창 전환 중 <b>더 최근</b> 쪽을 쓴다
    /// (창을 바꾸면 그 시점부터 유휴를 다시 센다).
    /// </summary>
    public static long MillisSinceActivity(long millisSinceLastInput, long millisSinceForegroundChange)
        => Math.Min(millisSinceLastInput, millisSinceForegroundChange);

    /// <summary>스냅샷 하나를 받아 인디케이터를 어떻게 그릴지 결정한다.</summary>
    /// <param name="idleReappearMs">입력 후 다시 표시되기까지의 유휴 시간(ms). 사용자 설정값.</param>
    public static IndicatorView Decide(InputSnapshot s, long idleReappearMs = DefaultIdleReappearMs)
    {
        var state = ResolveState(s.KeyboardLangId, s.ConversionMode);
        var label = Label(state, s.KeyboardLangId);

        if (!ShouldShow(s.EditableFocus, s.MillisSinceInputActivity, idleReappearMs))
            return IndicatorView.Hidden;

        // 캐럿을 얻으면 캐럿 오른쪽에(방금 친 글자를 안 가리게), 못 얻으면(크롬 등)
        // 활성 창 우상단에 폴백(Ticket 04). 캐럿이라기엔 넓은 rect(입력 박스·선택 영역)면
        // 오른쪽 끝이 아니라 왼쪽(실제 캐럿 근처) 바깥에 붙인다.
        Point desired = s.Caret is Rectangle caret
            ? caret.Width <= MaxCaretWidth
                ? new Point(caret.Right + CaretGapX, caret.Top)
                : new Point(caret.Left - CaretGapX - s.IndicatorSize.Width, caret.Top)
            : FallbackTopRight(s.ActiveWindowBounds, s.IndicatorSize);

        return new IndicatorView(true, label, ClampToScreen(desired, s.IndicatorSize, s.ScreenBounds));
    }

    // 활성 창 우상단 안쪽.
    private static Point FallbackTopRight(Rectangle window, Size size)
        => new(window.Right - size.Width - FallbackMargin, window.Top + FallbackMargin);

    // 인디케이터가 화면 밖으로 잘리지 않도록 대상 화면 경계 안으로 보정.
    private static Point ClampToScreen(Point desired, Size size, Rectangle screen)
    {
        int maxX = Math.Max(screen.Left, screen.Right - size.Width);
        int maxY = Math.Max(screen.Top, screen.Bottom - size.Height);
        int x = Math.Min(Math.Max(desired.X, screen.Left), maxX);
        int y = Math.Min(Math.Max(desired.Y, screen.Top), maxY);
        return new Point(x, y);
    }
}
