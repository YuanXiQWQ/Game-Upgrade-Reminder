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
    /// <summary>
    /// RTL（从右到左）语言支持辅助工具类，用于处理阿拉伯语、希伯来语等RTL语言的界面布局
    /// </summary>
    public static class RtlHelper
    {
        /// <summary>
        /// RTL语言代码集合，包含需要从右到左显示的语言
        /// </summary>
        private static readonly HashSet<string> RtlLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", // 阿拉伯语
            "fa", // 波斯语
            "he", // 希伯来语
            "ur", // 乌尔都语
        };

        /// <summary>
        /// 判断给定的语言代码是否为RTL（从右到左）语言
        /// </summary>
        /// <param name="languageCode">语言代码（如：ar-SA, he-IL, zh-CN）</param>
        /// <returns>如果是RTL语言则返回true，否则返回false</returns>
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

        /// <summary>
        /// 根据语言代码为指定的控件树应用RTL布局设置
        /// </summary>
        /// <param name="root">根控件，将递归应用到其所有子控件</param>
        /// <param name="languageCode">语言代码，用于判断是否需要RTL布局</param>
        public static void Apply(Control root, string languageCode)
        {
            var rtl = IsRtlLanguage(languageCode);
            ApplyToControlTree(root, rtl);
        }

        /// <summary>
        /// 应用RTL布局并绑定语言变更事件，当语言切换时自动更新RTL设置
        /// </summary>
        /// <param name="localization">本地化服务实例，用于获取当前语言和监听语言变更</param>
        /// <param name="root">根控件，将递归应用到其所有子控件</param>
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

        /// <summary>
        /// 递归地为控件树中的所有控件应用RTL布局设置
        /// </summary>
        /// <param name="c">要处理的控件</param>
        /// <param name="rtl">是否启用RTL布局</param>
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