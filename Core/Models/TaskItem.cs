/*
 * 游戏升级提醒 - 任务项模型
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务项的数据结构和相关操作
 * 创建日期: 2025-08-14
 * 最后修改: 2025-08-23
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
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
        /// 获取或设置一个值，指示最近一次提醒是否正等待用户点击“完成”进行确认。
        /// 用于控制行高亮与“暂停计时直到确认”的逻辑。
        /// </summary>
        public bool AwaitingAck { get; set; }

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
        /// 重复设置（为 null 或 Mode=None 表示不重复）
        /// </summary>
        public RepeatSpec? Repeat { get; set; }

        /// <summary>
        /// 已重复次数（提醒触发+1；跳过不+1；编辑任务后清零）
        /// </summary>
        public int RepeatCount { get; set; }

        /// <summary>
        /// 发生过的总次数光标（包括跳过与提醒），用于根据跳过规则推进周期。
        /// 注意：与 <see cref="RepeatCount"/> 不同，后者仅在实际提醒时+1。
        /// </summary>
        public int RepeatCursor { get; set; }

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