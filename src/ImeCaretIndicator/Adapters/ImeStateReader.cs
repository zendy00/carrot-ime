namespace ImeCaretIndicator.Adapters;

/// <summary>포그라운드 창 기준 키보드 레이아웃 언어 ID와 IME 조합 모드를 읽는다.</summary>
internal static class ImeStateReader
{
    public static (ushort LangId, uint ConversionMode) Read(IntPtr foreground, uint threadId)
    {
        IntPtr hkl = Win32.GetKeyboardLayout(threadId);
        ushort langId = (ushort)(hkl.ToInt64() & 0xFFFF);

        uint conversionMode = 0;
        IntPtr imeWnd = Win32.ImmGetDefaultIMEWnd(foreground);
        if (imeWnd != IntPtr.Zero)
        {
            // 크로스 프로세스로 현재 조합 모드를 질의 (한/영 상태)
            IntPtr result = Win32.SendMessage(
                imeWnd, Win32.WM_IME_CONTROL, (IntPtr)Win32.IMC_GETCONVERSIONMODE, IntPtr.Zero);
            conversionMode = (uint)result.ToInt64();
        }

        return (langId, conversionMode);
    }
}
