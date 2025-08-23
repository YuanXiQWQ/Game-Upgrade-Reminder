/*
 * 游戏升级提醒 - 本地化时长格式化器
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供本地化的时长格式化功能
 * 创建日期: 2025-08-22
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
    /// 实现本地化的持续时间格式化器
    /// </summary>
    public sealed class LocalizedDurationFormatter(ILocalizationService localizationService) : IDurationFormatter
    {
        /// <summary>
        /// 将时间间隔格式化为本地化字符串
        /// </summary>
        public string Format(int days, int hours, int minutes, int seconds = 0, bool showSeconds = false)
        {
            var d = days;
            var h = hours;
            var m = minutes;
            var s = seconds;
            DurationUtils.NormalizeDhms(ref d, ref h, ref m, ref s);

            if (d == 0 && h == 0 && m == 0 && (!showSeconds || s == 0))
            {
                return showSeconds
                    ? $"0{localizationService.GetText("Duration.Second", "s")}"
                    : $"0{localizationService.GetText("Duration.Minute", "m")}";
            }

            var parts = new List<string>();
            if (d > 0) parts.Add($"{d}{localizationService.GetText("Duration.Day", "d")}");
            if (h > 0) parts.Add($"{h}{localizationService.GetText("Duration.Hour", "h")}");
            if (m > 0) parts.Add($"{m}{localizationService.GetText("Duration.Minute", "m")}");
            if (showSeconds && s > 0) parts.Add($"{s}{localizationService.GetText("Duration.Second", "s")}");

            return string.Join(" ", parts);
        }
    }
}
