/*
 * 游戏升级提醒 - 按完成时间排序策略
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现按任务完成时间升序排序的策略类，用于任务列表的排序和插入
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.ComponentModel;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>与原逻辑一致：按 Finish 升序排序/插入。</summary>
    public sealed class ByFinishTimeSortStrategy : ISortStrategy
    {
        public void Sort(BindingList<TaskItem> tasks)
        {
            var list = new List<TaskItem>(tasks);
            list.Sort((a, b) => a.Finish.CompareTo(b.Finish));
            tasks.Clear();
            foreach (var t in list) tasks.Add(t);
        }

        public void Insert(BindingList<TaskItem> tasks, TaskItem item)
        {
            int i = 0;
            while (i < tasks.Count && tasks[i].Finish <= item.Finish) i++;
            tasks.Insert(i, item);
        }
    }
}