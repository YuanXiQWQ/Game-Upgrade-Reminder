/*
 * 游戏升级提醒 - 简单删除策略
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现任务删除策略，包括待删除延迟和完成后的保留时间
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 实现简单的任务删除策略，支持延迟删除和完成后保留时间
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="IDeletionPolicy"/>接口，
    /// 提供基于时间的任务删除策略，包括：
    /// 1. 标记为删除的任务在经过指定延迟后删除
    /// 2. 已完成的任务在保留指定时间后删除
    /// </remarks>
    /// <param name="pendingDeleteDelaySeconds">标记为删除后的延迟秒数，默认为3秒</param>
    /// <param name="completedKeepSeconds">任务完成后保留的秒数，默认为60秒</param>
    public sealed class SimpleDeletionPolicy(int pendingDeleteDelaySeconds = 3, int completedKeepSeconds = 60) : IDeletionPolicy
    {
        /// <summary>
        /// 获取标记为删除后的延迟秒数
        /// </summary>
        public int PendingDeleteDelaySeconds { get; } = pendingDeleteDelaySeconds;

        /// <summary>
        /// 获取任务完成后保留的秒数
        /// </summary>
        public int CompletedKeepSeconds { get; } = completedKeepSeconds;

        /// <summary>
        /// 确定是否应该删除指定的任务
        /// </summary>
        /// <param name="task">要检查的任务</param>
        /// <param name="now">当前时间</param>
        /// <param name="force">是否强制删除，忽略时间限制</param>
        /// <returns>如果应该删除任务则返回true，否则返回false</returns>
        /// <remarks>
        /// 以下情况会返回true：
        /// 1. 任务已标记为删除，并且：
        ///    - force为true，或者
        ///    - 自标记时间起已超过<see cref="PendingDeleteDelaySeconds"/>秒
        /// 2. 任务已完成，并且：
        ///    - 自完成时间起已超过<see cref="CompletedKeepSeconds"/>秒
        /// </remarks>
        public bool ShouldRemove(TaskItem task, DateTime now, bool force)
        {
            if (task.PendingDelete)
            {
                var mark = task.DeleteMarkTime ?? DateTime.MinValue;
                if (force || (now - mark).TotalSeconds >= PendingDeleteDelaySeconds)
                    return true;
            }
            else if (task is { Done: true, CompletedTime: not null } &&
                     (now - task.CompletedTime.Value).TotalSeconds >= CompletedKeepSeconds)
            {
                return true;
            }
            return false;
        }
    }
}