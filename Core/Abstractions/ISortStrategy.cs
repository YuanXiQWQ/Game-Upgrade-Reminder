/*
 * 游戏升级提醒 - 排序策略接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务排序策略的接口，用于实现不同的任务排序和插入逻辑
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.ComponentModel;
using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 定义任务排序策略的接口，用于对任务列表进行排序和插入操作
    /// </summary>
    public interface ISortStrategy
    {
        /// <summary>
        /// 对任务列表进行排序
        /// </summary>
        /// <param name="tasks">要排序的任务列表</param>
        /// <remarks>
        /// 此方法会直接修改传入的任务列表，按照实现类定义的排序规则进行排序。
        /// 实现应确保排序的稳定性和效率。
        /// </remarks>
        void Sort(BindingList<TaskItem> tasks);

        /// <summary>
        /// 将新任务插入到已排序列表中的适当位置
        /// </summary>
        /// <param name="tasks">已排序的任务列表</param>
        /// <param name="item">要插入的新任务项</param>
        /// <remarks>
        /// 此方法假设输入的任务列表已经按照特定顺序排序。
        /// 实现应找到新任务的正确位置并插入，以保持列表的排序状态。
        /// 如果列表未排序，结果可能不符合预期。
        /// </remarks>
        void Insert(BindingList<TaskItem> tasks, TaskItem item);
    }
}