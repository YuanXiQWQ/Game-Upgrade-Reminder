/*
 * 游戏升级提醒 - RTL 辅助工具
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-23
 * 最后修改: 2025-08-23
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    public static class RtlHelper
    {
        private static readonly HashSet<string> RtlLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", // 阿拉伯语
            "fa", // 波斯语
            "he", // 希伯来语
            "ur", // 乌尔都语
            "ps", // 普什图语
            "sd", // 信德语
            "syr", // 叙利亚语
            "ug", // 维吾尔语（阿拉伯字母）
            "yi", // 意第绪语
            "dv", // 迪维希语
            "ku-Arab" // 库尔德语（阿拉伯字母变体）
        };

        public static bool IsRtlLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return false;
            try
            {
                // 解析如 zh-CN、ar-SA 的前缀
                var tag = languageCode.Replace('_', '-');
                var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return false;
                var lang = parts[0];
                if (RtlLanguages.Contains(lang)) return true;

                // 特例：ku-Arab 等带脚本的标签
                if (parts.Length >= 2 && RtlLanguages.Contains($"{lang}-{parts[1]}"))
                    return true;
            }
            catch
            {
                // 忽略
            }

            return false;
        }

        public static void Apply(Control root, string languageCode)
        {
            var rtl = IsRtlLanguage(languageCode);
            ApplyToControlTree(root, rtl);
        }

        public static void ApplyAndBind(ILocalizationService localization, Control root)
        {
            Apply(root, localization.CurrentLanguage);
            localization.LanguageChanged += (_, _) =>
            {
                try
                {
                    Apply(root, localization.CurrentLanguage);
                }
                catch
                {
                    // 忽略
                }
            };
        }

        private static void ApplyToControlTree(Control c, bool rtl)
        {
            try
            {
                c.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
                switch (c)
                {
                    case Form f:
                        f.RightToLeftLayout = rtl;
                        break;
                    case ListView lv:
                        lv.RightToLeftLayout = rtl;
                        break;
                    case FlowLayoutPanel flp:
                        flp.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                        break;
                }

                foreach (Control child in c.Controls)
                    ApplyToControlTree(child, rtl);
            }
            catch
            {
                // 忽略
            }
        }
    }
}