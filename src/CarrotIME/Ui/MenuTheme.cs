using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CarrotIME.Ui;

/// <summary>
/// 윈도우 다크/라이트 테마에 맞춰 트레이 컨텍스트 메뉴 색을 바꾼다.
/// WinForms 메뉴는 기본이 라이트라, 다크일 때 직접 어두운 렌더러·색을 입힌다.
/// </summary>
internal static class MenuTheme
{
    private static readonly Color DarkBg = Color.FromArgb(43, 43, 43);
    private static readonly Color DarkText = Color.FromArgb(240, 240, 240);

    /// <summary>메뉴가 열릴 때마다 호출 — 현재 테마에 맞춰 색을 적용한다(실행 중 테마 전환도 반영).</summary>
    public static void Apply(ContextMenuStrip menu)
    {
        bool dark = IsDarkMode();
        ToolStripManager.Renderer = dark
            ? new ToolStripProfessionalRenderer(new DarkColorTable())
            : new ToolStripProfessionalRenderer();

        menu.BackColor = dark ? DarkBg : SystemColors.Menu;
        StyleItems(menu.Items, dark);
    }

    private static void StyleItems(ToolStripItemCollection items, bool dark)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = dark ? DarkText : SystemColors.MenuText;
            // 텍스트박스(직접 입력)는 기본 흰 배경이라 다크에선 배경도 어둡게 바꿔야 글자가 보인다.
            if (item is ToolStripTextBox box)
                box.BackColor = dark ? Color.FromArgb(62, 62, 64) : SystemColors.Window;
            // 슬라이더(투명도)도 기본 컨트롤 배경이라 메뉴 배경색으로 맞춘다.
            if (item is ToolStripControlHost host && host.Control is TrackBar track)
                track.BackColor = dark ? DarkBg : SystemColors.Menu;
            if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
            {
                mi.DropDown.BackColor = dark ? DarkBg : SystemColors.Menu;
                StyleItems(mi.DropDownItems, dark);
            }
        }
    }

    /// <summary>앱 메뉴 테마 기준: HKCU…\Personalize\AppsUseLightTheme (0 = 다크).</summary>
    private static bool IsDarkMode()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return false; // 못 읽으면 라이트로 간주
        }
    }

    /// <summary>다크 메뉴용 색상 테이블.</summary>
    private sealed class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color Bg = DarkBg;
        private static readonly Color Hover = Color.FromArgb(62, 62, 64);
        private static readonly Color Border = Color.FromArgb(82, 82, 84);

        public override Color ToolStripDropDownBackground => Bg;
        public override Color ImageMarginGradientBegin => Bg;
        public override Color ImageMarginGradientMiddle => Bg;
        public override Color ImageMarginGradientEnd => Bg;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Bg;
        public override Color MenuItemPressedGradientEnd => Bg;
        public override Color CheckBackground => Hover;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Hover;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
