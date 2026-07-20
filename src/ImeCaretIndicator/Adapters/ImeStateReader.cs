namespace ImeCaretIndicator.Adapters;

/// <summary>포그라운드 창 기준 키보드 레이아웃 언어 ID와 IME 조합 모드를 읽는다.</summary>
internal static class ImeStateReader
{
    public static (ushort LangId, uint ConversionMode) Read(IntPtr foreground, uint threadId)
    {
        IntPtr hkl = Win32.GetKeyboardLayout(threadId);
        ushort langId = (ushort)(hkl.ToInt64() & 0xFFFF);

        // 한/영(조합 모드)은 포커스된 편집 컨트롤에 붙는다 → 포커스 창 기준으로 질의.
        // (최상위 foreground 창으로 물으면 조합 모드를 못 얻는 경우가 있어 늘 '영'으로 떨어짐)
        IntPtr target = foreground;
        if (Win32.TryGetGuiThreadInfo(threadId, out var gti) && gti.hwndFocus != IntPtr.Zero)
            target = gti.hwndFocus;

        uint conversionMode = 0;
        IntPtr imeWnd = Win32.ImmGetDefaultIMEWnd(target);
        if (imeWnd != IntPtr.Zero)
        {
            // 크로스 프로세스로 현재 조합 모드를 질의. 멈춘 앱이 UI 스레드를 물지 않도록
            // 타임아웃 + ABORTIFHUNG 사용.
            if (Win32.SendMessageTimeout(
                    imeWnd, Win32.WM_IME_CONTROL, (IntPtr)Win32.IMC_GETCONVERSIONMODE, IntPtr.Zero,
                    Win32.SMTO_ABORTIFHUNG, 200, out IntPtr result) != IntPtr.Zero)
            {
                conversionMode = (uint)result.ToInt64();
            }
        }

        ImeDebug.TraceOnChange(langId, conversionMode);
        return (langId, conversionMode);
    }
}
