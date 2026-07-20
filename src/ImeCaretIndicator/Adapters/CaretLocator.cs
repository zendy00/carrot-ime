using System.Drawing;
using System.Runtime.InteropServices;

namespace ImeCaretIndicator.Adapters;

/// <summary>
/// 캐럿 위치를 화면 좌표 사각형으로 얻는다. Ticket 01은 GetGUIThreadInfo만 사용.
/// (UIA·MSAA 다단 폴백은 Ticket 02에서 추가)
/// </summary>
internal static class CaretLocator
{
    public static Rectangle? TryGetCaretRect(uint threadId)
    {
        var gti = new Win32.GUITHREADINFO { cbSize = Marshal.SizeOf<Win32.GUITHREADINFO>() };
        if (!Win32.GetGUIThreadInfo(threadId, ref gti) || gti.hwndCaret == IntPtr.Zero)
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
