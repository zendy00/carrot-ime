using System.Drawing;

namespace ImeCaretIndicator.Core;

/// <summary>
/// 유일한 seam. 모든 결정 로직(상태 판별·라벨·위치·표시 여부)이 이 순수 함수 뒤에 모인다.
/// OS 접근은 전혀 하지 않는다 — 오직 InputSnapshot 값만 보고 IndicatorView 값을 낸다.
/// </summary>
public static class Decider
{
    // Win32 IME 조합 모드 플래그: 한글(네이티브) 입력 여부.
    private const uint ImeCmodeNative = 0x0001;

    // PRIMARYLANGID 마스크와 LANG_KOREAN.
    private const ushort PrimaryLangMask = 0x03ff;
    private const ushort LangKorean = 0x0012;

    // 캐럿과 인디케이터 사이 간격 — 방금 친 글자를 가리지 않도록.
    private const int CaretGapX = 4;

    // 고정 폴백 위치(활성 창 우상단)의 안쪽 여백.
    private const int FallbackMargin = 6;

    /// <summary>키보드 레이아웃 언어 ID와 IME 조합 모드로 입력 상태를 판별한다. (Ticket 01: 한글/영문)</summary>
    public static InputState ResolveState(ushort keyboardLangId, uint conversionMode)
    {
        bool korean = (keyboardLangId & PrimaryLangMask) == LangKorean;
        if (korean && (conversionMode & ImeCmodeNative) != 0)
            return InputState.Hangul;
        return InputState.English;
    }

    /// <summary>입력 상태에 대응하는 인디케이터 라벨.</summary>
    public static string Label(InputState state) => state switch
    {
        InputState.Hangul => "한",
        InputState.English => "영",
        _ => "IME"
    };

    /// <summary>스냅샷 하나를 받아 인디케이터를 어떻게 그릴지 결정한다.</summary>
    public static IndicatorView Decide(InputSnapshot s)
    {
        var state = ResolveState(s.KeyboardLangId, s.ConversionMode);
        var label = Label(state);

        // Ticket 03: 편집 가능한 텍스트 포커스가 있을 때만 표시.
        if (!s.EditableFocus)
            return IndicatorView.Hidden;

        // 캐럿을 얻으면 캐럿 오른쪽에, 못 얻으면(크롬 등) 활성 창 우상단에 폴백(Ticket 04).
        Point desired = s.Caret is Rectangle caret
            ? new Point(caret.Right + CaretGapX, caret.Top)
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
