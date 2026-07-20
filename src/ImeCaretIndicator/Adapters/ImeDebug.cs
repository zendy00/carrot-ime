using ImeCaretIndicator.Core;

namespace ImeCaretIndicator.Adapters;

/// <summary>
/// 진단용: 환경변수 IME_INDICATOR_DEBUG=1 일 때, langId/조합모드가 바뀔 때마다
/// %TEMP%\ime-indicator-debug.log 에 한 줄씩 남긴다. (버그 재현이 어려운 상황에서
/// 실제 값을 확인하기 위한 최소 관측 수단 — 평소엔 완전히 꺼짐)
/// </summary>
internal static class ImeDebug
{
    private static readonly bool Enabled =
        Environment.GetEnvironmentVariable("IME_INDICATOR_DEBUG") == "1";

    private static readonly string LogPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ime-indicator-debug.log");

    private static long _last = -1;

    public static void TraceOnChange(ushort langId, uint conversionMode)
    {
        if (!Enabled)
            return;

        long key = ((long)langId << 32) | conversionMode;
        if (key == _last)
            return;
        _last = key;

        var state = Decider.ResolveState(langId, conversionMode);
        try
        {
            System.IO.File.AppendAllText(
                LogPath,
                $"langId=0x{langId:X4} conv=0x{conversionMode:X4} -> {state}{Environment.NewLine}");
        }
        catch
        {
            // 로그 실패는 무시 (진단용 부가 기능)
        }
    }
}
