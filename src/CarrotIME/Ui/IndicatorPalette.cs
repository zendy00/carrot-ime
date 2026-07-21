using System.Drawing;

namespace CarrotIME.Ui;

/// <summary>인디케이터(캐럿 옆 원) 배경색으로 고를 수 있는 파스텔 팔레트.</summary>
internal static class IndicatorPalette
{
    public static readonly (string Name, Color Color)[] Swatches =
    {
        ("블루", Color.FromArgb(108, 142, 214)),
        ("퍼플", Color.FromArgb(155, 121, 196)),
        ("핑크", Color.FromArgb(201, 123, 180)),
        ("코랄", Color.FromArgb(224, 142, 109)),
        ("브라운", Color.FromArgb(181, 131, 90)),
        ("골드", Color.FromArgb(203, 164, 61)),
        ("올리브", Color.FromArgb(147, 172, 91)),
        ("그린", Color.FromArgb(109, 191, 154)),
        ("틸", Color.FromArgb(95, 182, 188)),
    };

    public static Color Default => Swatches[0].Color;
}
