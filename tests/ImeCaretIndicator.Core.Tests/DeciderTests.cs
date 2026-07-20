using System.Drawing;
using ImeCaretIndicator.Core;
using Xunit;

namespace ImeCaretIndicator.Core.Tests;

public class DeciderTests
{
    // Korean layout id = 0x0412, English (US) = 0x0409.
    private const ushort Ko = 0x0412;
    private const ushort En = 0x0409;
    private const uint Native = 0x0001; // IME_CMODE_NATIVE (한글 조합)
    private const uint Alpha = 0x0000;  // 영문 모드

    private static InputSnapshot Snap(Rectangle? caret, ushort lang = En, uint mode = Alpha, bool editable = true)
        => new(
            EditableFocus: editable,
            Caret: caret,
            ScreenBounds: new Rectangle(0, 0, 1920, 1080),
            IndicatorSize: new Size(24, 20),
            KeyboardLangId: lang,
            ConversionMode: mode);

    // ---- 상태 판별 ----

    [Fact]
    public void Korean_layout_in_native_mode_is_Hangul()
        => Assert.Equal(InputState.Hangul, Decider.ResolveState(Ko, Native));

    [Fact]
    public void Korean_layout_in_alphanumeric_mode_is_English()
        => Assert.Equal(InputState.English, Decider.ResolveState(Ko, Alpha));

    [Fact]
    public void Non_korean_layout_is_English_even_in_native_bit()
        => Assert.Equal(InputState.English, Decider.ResolveState(En, Native));

    // ---- 라벨 ----

    [Fact]
    public void Hangul_label_is_han() => Assert.Equal("한", Decider.Label(InputState.Hangul));

    [Fact]
    public void English_label_is_yeong() => Assert.Equal("영", Decider.Label(InputState.English));

    // ---- 위치 · 표시 여부 ----

    [Fact]
    public void Caret_present_shows_label_to_the_right_of_caret()
    {
        var view = Decider.Decide(Snap(new Rectangle(100, 200, 2, 16), Ko, Native));

        Assert.True(view.Visible);
        Assert.Equal("한", view.Label);
        // caret.Right(102) + gap(4) = 106, caret.Top = 200
        Assert.Equal(new Point(106, 200), view.Position);
    }

    [Fact]
    public void Caret_near_right_edge_is_clamped_within_screen()
    {
        var view = Decider.Decide(Snap(new Rectangle(1915, 200, 2, 16), En, Alpha));

        Assert.True(view.Visible);
        Assert.True(view.Position.X + 24 <= 1920, "indicator must not overflow the screen right edge");
    }

    [Fact]
    public void No_caret_hides_indicator()
    {
        var view = Decider.Decide(Snap(caret: null));
        Assert.False(view.Visible);
    }

    [Fact]
    public void No_editable_focus_hides_indicator_even_when_a_caret_is_present()
    {
        // 편집 포커스가 아니면(바탕화면·버튼 등) 캐럿이 있어도 숨긴다.
        var view = Decider.Decide(Snap(new Rectangle(100, 200, 2, 16), Ko, Native, editable: false));
        Assert.False(view.Visible);
    }
}
