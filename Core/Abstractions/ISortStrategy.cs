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
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public interface ISortStrategy
    {
        void Sort(BindingList<TaskItem> tasks);
        void Insert(BindingList<TaskItem> tasks, TaskItem item);
    }
}