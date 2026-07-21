using System.Drawing;
using ImeCaretIndicator.Core;
using Xunit;

namespace ImeCaretIndicator.Core.Tests;

public class DeciderTests
{
    // Korean layout id = 0x0412, English (US) = 0x0409, Japanese = 0x0411, Chinese(PRC) = 0x0804.
    private const ushort Ko = 0x0412;
    private const ushort En = 0x0409;
    private const ushort Ja = 0x0411;
    private const ushort Zh = 0x0804;
    private const uint Native = 0x0001; // IME_CMODE_NATIVE (한글 조합)
    private const uint Alpha = 0x0000;  // 영문 모드

    private static InputSnapshot Snap(
        Rectangle? caret, ushort lang = En, uint mode = Alpha, bool editable = true,
        Rectangle? activeWindow = null, long millisSinceActivity = 120_000)
        => new(
            EditableFocus: editable,
            Caret: caret,
            ActiveWindowBounds: activeWindow ?? new Rectangle(200, 100, 800, 600),
            ScreenBounds: new Rectangle(0, 0, 1920, 1080),
            IndicatorSize: new Size(24, 20),
            KeyboardLangId: lang,
            ConversionMode: mode,
            MillisSinceInputActivity: millisSinceActivity);

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

    // ---- Ticket 05: 기타 IME (일본어·중국어 등) ----

    [Fact]
    public void Japanese_layout_is_other_ime()
        => Assert.Equal(InputState.OtherIme, Decider.ResolveState(Ja, Native));

    [Fact]
    public void Chinese_layout_is_other_ime()
        => Assert.Equal(InputState.OtherIme, Decider.ResolveState(Zh, Native));

    [Fact]
    public void Japanese_label_is_a() => Assert.Equal("あ", Decider.Label(InputState.OtherIme, Ja));

    [Fact]
    public void Chinese_label_is_zhong() => Assert.Equal("中", Decider.Label(InputState.OtherIme, Zh));

    [Fact]
    public void Unknown_ime_label_falls_back_to_IME()
        => Assert.Equal("IME", Decider.Label(InputState.OtherIme, 0x0439)); // 그 외 언어(예: 힌디)

    // ---- 트레이 라벨: 한글=가, 영문=A (오버레이의 한/영과 별개) ----

    [Fact]
    public void Tray_label_for_hangul_is_ga() => Assert.Equal("가", Decider.TrayLabel(InputState.Hangul, Ko));

    [Fact]
    public void Tray_label_for_english_is_A() => Assert.Equal("A", Decider.TrayLabel(InputState.English, En));

    [Fact]
    public void Tray_label_for_other_ime_matches_overlay_glyph()
    {
        Assert.Equal("あ", Decider.TrayLabel(InputState.OtherIme, Ja));
        Assert.Equal("中", Decider.TrayLabel(InputState.OtherIme, Zh));
        Assert.Equal("IME", Decider.TrayLabel(InputState.OtherIme, 0x0439));
    }

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

    // (구 "No_caret_hides_indicator"는 Ticket 04 고정 폴백 도입으로 폐기 —
    //  편집 포커스 + 캐럿 없음은 이제 숨김이 아니라 폴백 표시. 아래 04 테스트가 대체.)

    [Fact]
    public void No_editable_focus_hides_indicator_even_when_a_caret_is_present()
    {
        // 편집 포커스가 아니면(바탕화면·버튼 등) 캐럿이 있어도 숨긴다.
        var view = Decider.Decide(Snap(new Rectangle(100, 200, 2, 16), Ko, Native, editable: false));
        Assert.False(view.Visible);
    }

    // ---- Ticket 04: 캐럿 못 얻는 앱의 고정 위치 폴백 ----

    [Fact]
    public void Editable_focus_without_caret_falls_back_to_active_window_top_right()
    {
        var win = new Rectangle(200, 100, 800, 600); // right=1000, top=100
        var view = Decider.Decide(Snap(caret: null, lang: Ko, mode: Native, activeWindow: win));

        Assert.True(view.Visible);
        Assert.Equal("한", view.Label);
        // 우상단: 창 오른쪽에서 인디케이터 폭+여백만큼 안쪽, 위에서 여백만큼 아래
        Assert.True(view.Position.X < win.Right, "우상단이므로 창 오른쪽 경계 안이어야 한다");
        Assert.True(view.Position.X > win.Left + win.Width / 2, "우측 절반에 있어야 한다");
        Assert.True(view.Position.Y >= win.Top && view.Position.Y < win.Top + 100, "상단 근처여야 한다");
    }

    [Fact]
    public void Fallback_position_is_clamped_within_screen()
    {
        // 활성 창이 화면 오른쪽 밖으로 걸쳐 있어도 인디케이터는 화면 안에 있어야 한다.
        var win = new Rectangle(1800, 0, 400, 300); // right=2200 (화면 밖)
        var view = Decider.Decide(Snap(caret: null, editable: true, activeWindow: win));

        Assert.True(view.Visible);
        Assert.True(view.Position.X + 24 <= 1920, "인디케이터가 화면 오른쪽 밖으로 나가면 안 된다");
    }

    [Fact]
    public void No_editable_focus_and_no_caret_still_hidden()
    {
        var view = Decider.Decide(Snap(caret: null, editable: false));
        Assert.False(view.Visible);
    }

    // ---- Debounce: 입력 중엔 숨기고, 약 20초 이상 유휴면 다시 표시 ----

    [Fact]
    public void Recent_input_activity_hides_indicator_while_typing()
    {
        // 방금(0.1초 전) 캐럿이 움직임 = 입력 중 → 숨김
        var view = Decider.Decide(
            Snap(new Rectangle(100, 200, 2, 16), Ko, Native, millisSinceActivity: 100));
        Assert.False(view.Visible);
    }

    [Fact]
    public void Still_hidden_before_idle_threshold()
    {
        // 10초 유휴는 아직 20초 미만 → 숨김
        var view = Decider.Decide(
            Snap(new Rectangle(100, 200, 2, 16), Ko, Native, millisSinceActivity: 10_000));
        Assert.False(view.Visible);
    }

    [Fact]
    public void Indicator_reappears_after_idle_threshold()
    {
        // 20초 유휴 → 표시(기본 임계값 20초)
        var view = Decider.Decide(
            Snap(new Rectangle(100, 200, 2, 16), Ko, Native, millisSinceActivity: 20_000));
        Assert.True(view.Visible);
        Assert.Equal("한", view.Label);
    }

    [Fact]
    public void Custom_idle_threshold_is_respected()
    {
        var snap = Snap(new Rectangle(100, 200, 2, 16), Ko, Native, millisSinceActivity: 15_000);

        // 기본(20초) 임계에선 15초 유휴는 아직 숨김
        Assert.False(Decider.Decide(snap).Visible);
        // 사용자가 10초로 설정하면 15초 유휴는 표시
        Assert.True(Decider.Decide(snap, idleReappearMs: 10_000).Visible);
    }
}
