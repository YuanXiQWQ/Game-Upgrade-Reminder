/*
 * 游戏升级提醒 - 时间单位整理
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义时间单位整理方法
 * 创建日期: 2025-08-22
 * 最后修改: 2025-08-22
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */


namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 时间单位整理方法
    /// </summary>
    internal static class DurationUtils
    {
        /// <summary>
        /// 将秒/分/时进位到分/时/天，并保证：0 &lt;= seconds &lt; 60，0 &lt;= minutes &lt; 60，0 &lt;= hours &lt; 24。
        /// 不处理天以上的进位（避免月份天数不确定）。
        /// </summary>
        public static void NormalizeDhms(ref int days, ref int hours, ref int minutes, ref int seconds)
        {
            seconds = Math.Max(0, seconds);
            minutes = Math.Max(0, minutes);
            hours = Math.Max(0, hours);
            days = Math.Max(0, days);

            if (seconds >= 60)
            {
                minutes += seconds / 60;
                seconds %= 60;
            }

            if (minutes >= 60)
            {
                hours += minutes / 60;
                minutes %= 60;
            }

            if (hours >= 24)
            {
                days += hours / 24;
                hours %= 24;
            }
        }

        /// <summary>
        /// 在 <see cref="NormalizeDhms"/> 基础上，将月数进位为年：保证 0 &lt;= months &lt; 12。
        /// 不进行天-&gt;月进位（避免每月天数差异引起歧义）。
        /// </summary>
        public static void NormalizeYmDhms(ref int years, ref int months, ref int days, ref int hours, ref int minutes,
            ref int seconds)
        {
            years = Math.Max(0, years);
            months = Math.Max(0, months);
            NormalizeDhms(ref days, ref hours, ref minutes, ref seconds);

            if (months < 12) return;
            years += months / 12;
            months %= 12;
        }
    }
}