/*
 * 游戏升级提醒 - 任务项模型
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务项的数据结构和相关操作
 * 创建日期: 2025-08-14
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Text.Json.Serialization;

namespace Game_Upgrade_Reminder.Core.Models
{
    /// <summary>
    /// 表示一个任务项，包含任务的详细信息、状态和计时信息
    /// </summary>
    public sealed class TaskItem
    {
        /// <summary>
        /// 默认账号名称
        /// </summary>
        public const string DefaultAccount = "Default";

        /// <summary>
        /// 获取或设置任务所属账号
        /// </summary>
        /// <value>默认为<see cref="DefaultAccount"/></value>
        public string Account { get; init; } = DefaultAccount;

        /// <summary>
        /// 获取或设置任务名称
        /// </summary>
        /// <value>默认为"-"</value>
        public string TaskName { get; init; } = "-";

        /// <summary>
        /// 获取或设置任务开始时间
        /// </summary>
        public DateTime? Start { get; init; }

        /// <summary>
        /// 获取或设置任务持续天数
        /// </summary>
        public int Days { get; init; }

        /// <summary>
        /// 获取或设置任务持续小时数
        /// </summary>
        public int Hours { get; init; }

        /// <summary>
        /// 获取或设置任务持续分钟数
        /// </summary>
        public int Minutes { get; init; }

        /// <summary>
        /// 获取或设置任务完成时间
        /// </summary>
        public DateTime Finish { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否已发送通知
        /// </summary>
        public bool Notified { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否已发送“提前通知”
        /// </summary>
        public bool AdvanceNotified { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示任务是否已完成
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// 获取或设置任务完成时间
        /// </summary>
        public DateTime? CompletedTime { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示任务是否标记为待删除
        /// </summary>
        public bool PendingDelete { get; set; }

        /// <summary>
        /// 获取或设置任务标记为删除的时间
        /// </summary>
        public DateTime? DeleteMarkTime { get; set; }

        /// <summary>
        /// 获取任务的剩余时间
        /// </summary>
        [JsonIgnore]
        public TimeSpan Remaining => Finish - DateTime.Now;

        /// <summary>
        /// 获取时间显示的格式字符串
        /// </summary>
        public static readonly string TimeFormat = "yyyy-MM-dd HH:mm";

        /// <summary>
        /// 获取开始时间的格式化字符串
        /// </summary>
        public string StartStr => Start.HasValue ? Start.Value.ToString(TimeFormat) : "";

        /// <summary>
        /// 获取完成时间的格式化字符串
        /// </summary>
        public string FinishStr => Finish.ToString(TimeFormat);

        /// <summary>
        /// 获取剩余时间的格式化字符串
        /// </summary>
        /// <remarks>
        /// 当剩余时间小于等于0时返回"到点"。
        /// 否则返回格式化的时间字符串，包含天、时、分、秒。
        /// </remarks>
        public string RemainingStr
        {
            get
            {
                var d = Remaining;
                if (d.TotalSeconds <= 0) return "到点";

                var days = (int)Math.Floor(d.TotalDays);
                var hours = (int)Math.Floor(d.TotalHours) % 24;
                var minutes = d.Minutes;
                var seconds = d.Seconds;

                return FormatTime(days, hours, minutes, showSeconds: true, seconds);
            }
        }

        /// <summary>
        /// 将时间格式化为可读字符串
        /// </summary>
        /// <param name="days">天数</param>
        /// <param name="hours">小时数</param>
        /// <param name="minutes">分钟数</param>
        /// <param name="showSeconds">是否显示秒数</param>
        /// <param name="seconds">秒数</param>
        /// <returns>格式化后的时间字符串</returns>
        /// <remarks>
        /// 根据时间长度自动选择合适的显示格式：
        /// - 大于1天：显示天、时、分
        /// - 大于1小时：显示时、分（可选秒）
        /// - 其他：显示分（可选秒）
        /// </remarks>
        public static string FormatTime(int days, int hours, int minutes, bool showSeconds = false, int seconds = 0)
        {
            if (days > 0) return $"{days}天 {hours}时 {minutes}分";

            if (hours > 0) return showSeconds ? $"{hours}时 {minutes}分 {seconds}秒" : $"{hours}时 {minutes}分";

            return showSeconds ? $"{minutes}分 {seconds}秒" : $"{minutes}分";
        }

        /// <summary>
        /// 根据开始时间和持续时间重新计算完成时间
        /// </summary>
        /// <remarks>
        /// 如果开始时间为null，则使用当前时间作为开始时间。
        /// 计算方式：开始时间 + 天数 + 小时数 + 分钟数
        /// </remarks>
        public void RecalcFinishFromStart()
        {
            var st = Start ?? DateTime.Now;
            Finish = st.AddDays(Days).AddHours(Hours).AddMinutes(Minutes);
        }
    }
}
