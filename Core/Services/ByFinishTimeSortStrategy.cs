/*
 * 游戏升级提醒 - 按完成时间排序策略
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现按任务完成时间升序排序的策略类，用于任务列表的排序和插入
 * 创建日期: 2025-08-15
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.ComponentModel;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 按任务完成时间升序排序的策略实现类
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="ISortStrategy"/>接口，
    /// 提供按<see cref="TaskItem.Finish">完成时间</see>升序排序的功能。
    /// </remarks>
    public sealed class ByFinishTimeSortStrategy : ISortStrategy
    {
        /// <summary>
        /// 对任务列表按完成时间进行升序排序
        /// </summary>
        /// <param name="tasks">要排序的任务列表</param>
        /// <remarks>
        /// 此方法会直接修改传入的任务列表。
        /// 排序是稳定的，即完成时间相同的任务会保持它们原有的相对顺序。
        /// </remarks>
        public void Sort(BindingList<TaskItem> tasks)
        {
            var now = DateTime.Now;
            var list = new List<TaskItem>(tasks);
            list.Sort((a, b) =>
            {
                var aDue = a.AwaitingAck || a.Finish <= now;
                var bDue = b.AwaitingAck || b.Finish <= now;

                // 先按分组（到点/等待确认 优先）
                if (aDue != bDue) return aDue ? -1 : 1;
                // 组内排序：
                // - 到点/待确认：优先按账号名称升序，再按完成时间（便于一次性处理同账号任务）
                // - 未到点：保持原有行为，按完成时间升序
                if (!aDue || !bDue) return a.Finish.CompareTo(b.Finish);

                var accCmp = string.Compare(a.Account, b.Account, StringComparison.Ordinal);
                return accCmp != 0 ? accCmp : a.Finish.CompareTo(b.Finish);
            });
            tasks.Clear();
            foreach (var t in list) tasks.Add(t);
        }

        /// <summary>
        /// 将新任务插入到已排序列表中的适当位置
        /// </summary>
        /// <param name="tasks">已按完成时间升序排序的任务列表</param>
        /// <param name="item">要插入的新任务项</param>
        /// <remarks>
        /// 此方法假设输入的任务列表已经按完成时间升序排序。
        /// 新任务将根据其完成时间插入到合适的位置，以保持列表的排序状态。
        /// 如果列表未排序，结果可能不符合预期。
        /// </remarks>
        public void Insert(BindingList<TaskItem> tasks, TaskItem item)
        {
            var now = DateTime.Now;

            var newDue = IsDue(item);
            var i = 0;
            for (; i < tasks.Count; i++)
            {
                var cur = tasks[i];
                var curDue = IsDue(cur);

                // 到点/等待确认 任务应排在未到点任务之前
                if (newDue && !curDue) break;

                // 同组内插入规则：
                // - 到点/待确认：先按账号升序，再按完成时间升序
                if (curDue != newDue) continue;

                if (newDue)
                {
                    var accCmp = string.Compare(item.Account, cur.Account, StringComparison.Ordinal);
                    if (accCmp < 0) break;
                    if (accCmp == 0 && cur.Finish > item.Finish) break;
                }
                else
                {
                    // 未到点：按完成时间升序
                    if (cur.Finish > item.Finish) break;
                }
            }

            tasks.Insert(i, item);
            return;

            bool IsDue(TaskItem t) => t.AwaitingAck || t.Finish <= now;
        }
    }
}