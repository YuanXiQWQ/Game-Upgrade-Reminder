/*
 * 游戏升级提醒 - 本地化日期格式服务实现
 * 按语言返回日期顺序（YMD/DMY/MDY），并提供统一的格式化与 DateTimePicker 自定义格式字符串。
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-24
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 本地化日期格式服务实现，根据语言提供不同的日期格式化方式
    /// </summary>
    /// <param name="localizationService">本地化服务实例</param>
    public sealed class LocalizedDateFormatService(ILocalizationService localizationService) : IDateFormatService
    {
        /// <summary>
        /// 日期顺序枚举，定义不同的日期组件排列方式
        /// </summary>
        private enum DateOrder
        {
            /// <summary>
            /// 年-月-日 格式（如：2025-01-15）
            /// </summary>
            Ymd,

            /// <summary>
            /// 日-月-年 格式（如：15-01-2025）
            /// </summary>
            Dmy,

            /// <summary>
            /// 月-日-年 格式（如：01-15-2025）
            /// </summary>
            Mdy
        }

        /// <summary>
        /// 根据语言代码获取对应的日期顺序格式
        /// </summary>
        /// <param name="languageCode">语言代码（如：zh-CN, en-US）</param>
        /// <returns>对应的日期顺序枚举值</returns>
        private static DateOrder GetDateOrder(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return DateOrder.Dmy;
            var tag = languageCode.Replace('_', '-').ToLowerInvariant();
            var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var lang = parts.Length > 0 ? parts[0] : tag;

            return lang switch
            {
                "zh" or "ja" or "ko" => DateOrder.Ymd,
                "en" => tag.StartsWith("en-us")
                    ? DateOrder.Mdy
                    : (tag.StartsWith("en-ca") ? DateOrder.Ymd : DateOrder.Dmy),
                _ => DateOrder.Dmy
            };
        }

        /// <summary>
        /// 获取DateTimePicker控件使用的日期格式字符串
        /// </summary>
        /// <returns>适用于DateTimePicker的日期格式字符串</returns>
        public string GetDatePickerDateFormat()
        {
            return GetDateOrder(localizationService.CurrentLanguage) switch
            {
                DateOrder.Ymd => "yyyy-MM-dd",
                DateOrder.Dmy => "dd-MM-yyyy",
                DateOrder.Mdy => "MM-dd-yyyy",
                _ => "dd-MM-yyyy"
            };
        }

        /// <summary>
        /// 获取DateTimePicker控件使用的日期时间格式字符串
        /// </summary>
        /// <returns>适用于DateTimePicker的日期时间格式字符串，时间部分固定为24小时制</returns>
        public string GetDatePickerDateTimeFormat()
        {
            // 24 小时制时间部分固定为 HH:mm
            return GetDatePickerDateFormat() + " HH:mm";
        }

        /// <summary>
        /// 根据当前语言格式化日期
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <param name="includeYear">是否包含年份</param>
        /// <returns>格式化后的日期字符串</returns>
        public string FormatDate(DateTime dt, bool includeYear)
        {
            var order = GetDateOrder(localizationService.CurrentLanguage);
            if (includeYear)
            {
                return order switch
                {
                    DateOrder.Ymd => dt.ToString("yyyy-MM-dd"),
                    DateOrder.Dmy => dt.ToString("dd-MM-yyyy"),
                    DateOrder.Mdy => dt.ToString("MM-dd-yyyy"),
                    _ => dt.ToString("dd-MM-yyyy")
                };
            }

            return order switch
            {
                DateOrder.Ymd => dt.ToString("MM-dd"),
                DateOrder.Dmy => dt.ToString("dd-MM"),
                DateOrder.Mdy => dt.ToString("MM-dd"),
                _ => dt.ToString("dd-MM")
            };
        }

        /// <summary>
        /// 格式化时间部分，使用24小时制
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <param name="includeSeconds">是否包含秒数</param>
        /// <returns>格式化后的时间字符串</returns>
        public string FormatTime(DateTime dt, bool includeSeconds)
        {
            return dt.ToString(includeSeconds ? "H:mm:ss" : "H:mm");
        }

        /// <summary>
        /// 格式化完整的日期时间
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <param name="includeYear">是否包含年份</param>
        /// <param name="includeSeconds">是否包含秒数</param>
        /// <returns>格式化后的日期时间字符串</returns>
        public string FormatDateTime(DateTime dt, bool includeYear, bool includeSeconds)
        {
            return $"{FormatDate(dt, includeYear)} {FormatTime(dt, includeSeconds)}";
        }

        /// <summary>
        /// 智能格式化日期时间，根据与当前时间的关系自动调整显示格式
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <param name="now">当前时间，用于比较</param>
        /// <returns>智能格式化后的日期时间字符串。如果是同一天则只显示时间，否则显示完整日期时间</returns>
        public string FormatSmartDateTime(DateTime dt, DateTime now)
        {
            var time = FormatTime(dt, includeSeconds: dt.Second != 0);
            if (dt.Date == now.Date) return time;

            var includeYear = dt.Year != now.Year;
            return $"{FormatDate(dt, includeYear)} {time}";
        }
    }
}