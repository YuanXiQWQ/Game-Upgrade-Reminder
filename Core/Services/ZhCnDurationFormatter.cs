/*
 * 游戏升级提醒 - 中文时长格式化器
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供中文格式的时长格式化功能，用于显示游戏升级剩余时间
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 实现中文格式的持续时间格式化器
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="IDurationFormatter"/>接口，
    /// 提供将时间间隔格式化为中文表示的功能。
    /// 格式示例："2天 3时 5分 30秒" 或 "1时 45分"
    /// </remarks>
    public sealed class ZhCnDurationFormatter : IDurationFormatter
    {
        /// <summary>
        /// 将时间间隔格式化为中文字符串
        /// </summary>
        /// <param name="days">天数</param>
        /// <param name="hours">小时数</param>
        /// <param name="minutes">分钟数</param>
        /// <param name="seconds">秒数，默认为0</param>
        /// <param name="showSeconds">是否显示秒数，默认为false</param>
        /// <returns>格式化后的中文字符串</returns>
        /// <remarks>
        /// 格式化规则：
        /// 1. 只显示非零的时间单位
        /// 2. 如果showSeconds为true，则始终显示秒数
        /// 3. 时间单位之间用空格分隔
        /// 4. 如果所有时间单位都为零，则返回"0分"或"0秒"（根据showSeconds参数）
        /// </remarks>
        public string Format(int days, int hours, int minutes, int seconds = 0, bool showSeconds = false)
        {
            var parts = new List<string>();

            if (days > 0)
            {
                parts.Add($"{days}天");
                if (hours > 0 || (hours == 0 && (minutes > 0 || (showSeconds && seconds > 0))))
                {
                    parts.Add($"{hours}时");
                    if (minutes > 0 || (showSeconds && seconds > 0))
                    {
                        parts.Add($"{minutes}分");
                        if (showSeconds && seconds > 0)
                        {
                            parts.Add($"{seconds}秒");
                        }
                    }
                    else if (showSeconds && seconds > 0)
                    {
                        parts.Add($"0分 {seconds}秒");
                    }
                }
                else if (minutes > 0 || (showSeconds && seconds > 0))
                {
                    parts.Add($"0时 {minutes}分");
                    if (showSeconds && seconds > 0)
                    {
                        parts.Add($"{seconds}秒");
                    }
                }
                else if (showSeconds && seconds > 0)
                {
                    parts.Add($"0时 0分 {seconds}秒");
                }
            }
            else if (hours > 0)
            {
                parts.Add($"{hours}时");
                if (minutes > 0 || (showSeconds && seconds > 0))
                {
                    parts.Add($"{minutes}分");
                    if (showSeconds && seconds > 0)
                    {
                        parts.Add($"{seconds}秒");
                    }
                }
                else if (showSeconds && seconds > 0)
                {
                    parts.Add($"0分 {seconds}秒");
                }
            }
            else if (minutes > 0)
            {
                parts.Add($"{minutes}分");
                if (showSeconds && seconds > 0)
                {
                    parts.Add($"{seconds}秒");
                }
            }
            else if (showSeconds && seconds > 0)
            {
                return $"{seconds}秒";
            }
            else
            {
                return showSeconds ? "0秒" : "0分";
            }

            return string.Join(" ", parts);
        }
    }
}
