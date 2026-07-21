using System.Drawing;
using System.Runtime.InteropServices;
using Accessibility;

namespace CarrotIME.Adapters;

/// <summary>
/// MSAA(OBJID_CARET)로 캐럿 위치를 얻는다. 폴백 체인의 마지막 소스
/// (네이티브·UIA 모두 실패한 접근성 지원 앱 대비).
/// </summary>
internal static class MsaaCaret
{
    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(
        IntPtr hwnd, uint dwId, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

    private const uint OBJID_CARET = 0xFFFFFFF8;
    private const int CHILDID_SELF = 0;
    private static Guid IID_IAccessible = new("618736e0-3c3d-11cf-810c-00aa00389b71");

    public static Rectangle? TryGet(uint threadId)
    {
        try
        {
            IntPtr hwnd = FocusHwnd(threadId);
            if (hwnd == IntPtr.Zero)
                return null;

            int hr = AccessibleObjectFromWindow(hwnd, OBJID_CARET, ref IID_IAccessible, out object acc);
            if (hr != 0 || acc is not IAccessible accessible)
                return null;

            accessible.accLocation(out int left, out int top, out int width, out int height, CHILDID_SELF);
            if (width <= 0 && height <= 0)
                return null;

            return new Rectangle(left, top, Math.Max(1, width), Math.Max(1, height));
        }
        catch
        {
            // MSAA/COM 호출도 예외를 던질 수 있음 → 캐럿 못 얻은 것으로 보고 다음(없음)으로 폴백
            return null;
        }
    }

    private static IntPtr FocusHwnd(uint threadId)
    {
        if (!Win32.TryGetGuiThreadInfo(threadId, out var gti))
            return IntPtr.Zero;
        return gti.hwndFocus != IntPtr.Zero ? gti.hwndFocus : gti.hwndCaret;
    }
}
