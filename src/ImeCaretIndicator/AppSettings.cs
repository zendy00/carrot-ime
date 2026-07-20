using System.IO;

namespace ImeCaretIndicator;

/// <summary>
/// 사용자 설정 — 인디케이터 켜짐 여부와 자동 시작 선호. %APPDATA%에 key=value 텍스트로 저장.
/// (의존성·리플렉션 없이 AOT 배포에 안전하도록 JSON 대신 단순 텍스트 사용)
/// </summary>
internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    // 자동 시작의 실제 진실은 작업 스케줄러 등록 여부(AutoStart.IsEnabled). 여기엔 선호만 보관.
    public bool AutoStart { get; set; }

    private static string Dir => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImeCaretIndicator");

    private static string FilePath => System.IO.Path.Combine(Dir, "settings.txt");

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        try
        {
            if (!File.Exists(FilePath))
                return settings;

            foreach (string line in File.ReadAllLines(FilePath))
            {
                string[] kv = line.Split('=', 2);
                if (kv.Length != 2)
                    continue;
                switch (kv[0].Trim())
                {
                    case "Enabled": settings.Enabled = kv[1].Trim() == "1"; break;
                    case "AutoStart": settings.AutoStart = kv[1].Trim() == "1"; break;
                }
            }
        }
        catch
        {
            // 손상/접근 실패 시 기본값 사용
        }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, $"Enabled={(Enabled ? 1 : 0)}\nAutoStart={(AutoStart ? 1 : 0)}\n");
        }
        catch
        {
            // 저장 실패는 조용히 무시
        }
    }
}
