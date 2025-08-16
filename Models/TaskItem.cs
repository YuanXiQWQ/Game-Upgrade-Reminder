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

namespace Game_Upgrade_Reminder.Models
{
    public sealed class TaskItem
    {
        public const string DefaultAccount = "Default";
        public string Account { get; init; } = DefaultAccount;
        public string TaskName { get; init; } = "-";

        public DateTime? Start { get; init; }
        public int Days { get; init; }
        public int Hours { get; init; }
        public int Minutes { get; init; }

        public DateTime Finish { get; set; }
        public bool Notified { get; set; }
        public bool Done { get; set; }
        public DateTime? CompletedTime { get; set; }

        public bool PendingDelete { get; set; }
        public DateTime? DeleteMarkTime { get; set; }

        [JsonIgnore] public TimeSpan Remaining => Finish - DateTime.Now;

        public static readonly string TimeFormat = "yyyy-MM-dd HH:mm";

        public string StartStr => Start.HasValue ? Start.Value.ToString(TimeFormat) : "";
        public string FinishStr => Finish.ToString(TimeFormat);

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

        public static string FormatTime(int days, int hours, int minutes, bool showSeconds = false, int seconds = 0)
        {
            if (days > 0) return $"{days}天 {hours}时 {minutes}分";

            if (hours > 0) return showSeconds ? $"{hours}时 {minutes}分 {seconds}秒" : $"{hours}时 {minutes}分";

            return showSeconds ? $"{minutes}分 {seconds}秒" : $"{minutes}分";
        }

        public void RecalcFinishFromStart()
        {
            var st = Start ?? DateTime.Now;
            Finish = st.AddDays(Days).AddHours(Hours).AddMinutes(Minutes);
        }
    }
}