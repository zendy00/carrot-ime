using System.Drawing;
using System.Windows.Forms;
using ImeCaretIndicator.Adapters;

namespace ImeCaretIndicator.Ui;

/// <summary>
/// 캐럿 옆에 라벨 하나를 그리는 테두리 없는·최상단·클릭통과·비활성 오버레이 창.
/// 항상 100% 불투명(ADR/스펙: 항상 또렷).
/// </summary>
internal sealed class OverlayForm : Form
{
    private string _label = string.Empty;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(32, 32, 32);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        Size = new Size(26, 20);
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
        e.Graphics.Clear(BackColor);
        TextRenderer.DrawText(
            e.Graphics, _label, Font, ClientRectangle, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
