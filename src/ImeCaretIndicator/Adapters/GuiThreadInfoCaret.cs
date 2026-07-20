using System.Drawing;

namespace ImeCaretIndicator.Adapters;

/// <summary>네이티브 캐럿(GetGUIThreadInfo). 폴백 체인의 첫 번째 소스.</summary>
internal static class GuiThreadInfoCaret
{
    public static Rectangle? TryGet(uint threadId)
    {
        if (!Win32.TryGetGuiThreadInfo(threadId, out var gti) || gti.hwndCaret == IntPtr.Zero)
            return null;

        var r = gti.rcCaret;
        if (r.Right - r.Left <= 0 && r.Bottom - r.Top <= 0)
            return null;

        // rcCaret은 hwndCaret의 클라이언트 좌표 → 화면 좌표로 변환
        var topLeft = new Win32.POINT { X = r.Left, Y = r.Top };
        var bottomRight = new Win32.POINT { X = r.Right, Y = r.Bottom };
        Win32.ClientToScreen(gti.hwndCaret, ref topLeft);
        Win32.ClientToScreen(gti.hwndCaret, ref bottomRight);

        return new Rectangle(
            topLeft.X,
            topLeft.Y,
            Math.Max(1, bottomRight.X - topLeft.X),
            Math.Max(1, bottomRight.Y - topLeft.Y));
    }
}
