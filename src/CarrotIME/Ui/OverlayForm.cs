using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using CarrotIME.Adapters;

namespace CarrotIME.Ui;

/// <summary>
/// 캐럿 옆에 라벨 하나를 그리는 테두리 없는·최상단·클릭통과·비활성 오버레이 창.
/// macOS 입력 소스 표시기처럼 원형 배경 + 글자(각각 색은 팔레트에서 선택). 항상 100% 불투명.
/// </summary>
internal sealed class OverlayForm : Form
{
    private const int Diameter = 20;

    private string _label = string.Empty;
    private Color _textColor = IndicatorPalette.DefaultText;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = IndicatorPalette.Default;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        Size = new Size(Diameter, Diameter);
    }

    /// <summary>원형 배경색을 바꾼다(BackColor가 곧 원 색이며, 설정 시 자동 다시 그림).</summary>
    public void SetColor(Color color) => BackColor = color;

    /// <summary>글자색을 바꾼다.</summary>
    public void SetTextColor(Color color)
    {
        if (_textColor == color)
            return;
        _textColor = color;
        Invalidate();
    }

    public Size PreferredIndicatorSize => Size;

    // 표시할 때 포커스를 빼앗지 않도록
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT
                        | Win32.WS_EX_TOPMOST | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // 레이어드 창을 100% 불투명으로
        Win32.SetLayeredWindowAttributes(Handle, 0, 255, Win32.LWA_ALPHA);
        ApplyCircularRegion();
    }

    // 창 자체를 원형으로 클립 → 사각 모서리는 투명해지고 파란 동그라미만 남는다.
    private void ApplyCircularRegion()
    {
        using var path = new GraphicsPath();
        path.AddEllipse(0, 0, Diameter, Diameter);
        Region = new Region(path);
    }

    /// <summary>라벨/위치를 갱신하고 필요하면 표시한다.</summary>
    public void Render(string label, Point position)
    {
        if (_label != label)
        {
            _label = label;
            Invalidate();
        }
        Location = position;
        if (!Visible)
            Show();
    }

    public void HideIndicator()
    {
        if (Visible)
            Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // 원형 배경은 BackColor + 원형 Region 클립으로 이미 그려진다.
        // 글자(색은 팔레트에서 선택) 가운데 정렬
        using var text = new SolidBrush(_textColor);
        using var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(_label, Font, text, new RectangleF(0, 0, Diameter, Diameter), fmt);
    }
}
