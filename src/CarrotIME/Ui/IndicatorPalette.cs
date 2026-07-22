using System.Drawing;

namespace CarrotIME.Ui;

/// <summary>인디케이터(캐럿 옆 원) 배경색으로 고를 수 있는 파스텔 팔레트.</summary>
internal static class IndicatorPalette
{
    public static readonly (string Name, Color Color)[] Swatches =
    {
        ("Blue", Color.FromArgb(108, 142, 214)),
        ("Purple", Color.FromArgb(155, 121, 196)),
        ("Pink", Color.FromArgb(201, 123, 180)),
        ("Coral", Color.FromArgb(224, 142, 109)),
        ("Brown", Color.FromArgb(181, 131, 90)),
        ("Gold", Color.FromArgb(203, 164, 61)),
        ("Olive", Color.FromArgb(147, 172, 91)),
        ("Green", Color.FromArgb(109, 191, 154)),
        ("Teal", Color.FromArgb(95, 182, 188)),
    };

    // 기본색: 코랄
    public static Color Default => ColorByName("Coral");

    /// <summary>인디케이터 글자색으로 고를 수 있는 대표색 팔레트.</summary>
    public static readonly (string Name, Color Color)[] TextSwatches =
    {
        ("White", Color.White),
        ("Black", Color.Black),
        ("Navy", Color.FromArgb(28, 44, 84)),
        ("Yellow", Color.FromArgb(255, 220, 80)),
        ("Red", Color.FromArgb(200, 60, 60)),
    };

    // 기본 글자색: 흰색
    public static Color DefaultText => Color.White;

    private static Color ColorByName(string name)
    {
        foreach (var (n, c) in Swatches)
            if (n == name)
                return c;
        return Swatches[0].Color;
    }
}
