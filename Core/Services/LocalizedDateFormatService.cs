/*
 * 游戏升级提醒 - 本地化日期格式服务实现
 * 按语言返回日期顺序（YMD/DMY/MDY），并提供统一的格式化与 DateTimePicker 自定义格式字符串。
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-24
 * 最后修改: 2025-08-24
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    public sealed class LocalizedDateFormatService : IDateFormatService
    {
        private readonly ILocalizationService _loc;

        public LocalizedDateFormatService(ILocalizationService localizationService)
        {
            _loc = localizationService;
        }

        private enum DateOrder
        {
            Ymd,
            Dmy,
            Mdy
        }

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

        public string GetDatePickerDateFormat()
        {
            return GetDateOrder(_loc.CurrentLanguage) switch
            {
                DateOrder.Ymd => "yyyy-MM-dd",
                DateOrder.Dmy => "dd-MM-yyyy",
                DateOrder.Mdy => "MM-dd-yyyy",
                _ => "dd-MM-yyyy"
            };
        }

        public string GetDatePickerDateTimeFormat()
        {
            // 24 小时制时间部分固定为 HH:mm
            return GetDatePickerDateFormat() + " HH:mm";
        }

        public string FormatDate(DateTime dt, bool includeYear)
        {
            var order = GetDateOrder(_loc.CurrentLanguage);
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
            else
            {
                return order switch
                {
                    DateOrder.Ymd => dt.ToString("MM-dd"),
                    DateOrder.Dmy => dt.ToString("dd-MM"),
                    DateOrder.Mdy => dt.ToString("MM-dd"),
                    _ => dt.ToString("dd-MM")
                };
            }
        }

        public string FormatTime(DateTime dt, bool includeSeconds)
        {
            return dt.ToString(includeSeconds ? "H:mm:ss" : "H:mm");
        }

        public string FormatDateTime(DateTime dt, bool includeYear, bool includeSeconds)
        {
            return $"{FormatDate(dt, includeYear)} {FormatTime(dt, includeSeconds)}";
        }

        public string FormatSmartDateTime(DateTime dt, DateTime now)
        {
            var time = FormatTime(dt, includeSeconds: dt.Second != 0);
            if (dt.Date == now.Date) return time;

            var includeYear = dt.Year != now.Year;
            return $"{FormatDate(dt, includeYear)} {time}";
        }
    }
}