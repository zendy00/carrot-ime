using System.Drawing;

namespace ImeCaretIndicator.Core;

/// <summary>Decide의 출력 — 인디케이터를 그리는 데 필요한 평범한 값.</summary>
public readonly record struct IndicatorView(bool Visible, string Label, Point Position)
{
    public static readonly IndicatorView Hidden = new(false, string.Empty, Point.Empty);
}
