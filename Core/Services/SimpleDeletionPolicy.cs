/*
 * 游戏升级提醒 - 简单删除策略
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现任务删除策略，包括待删除延迟和完成后的保留时间
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>与原实现参数一致：待删延迟3秒；完成后保留1分钟。</summary>
    public sealed class SimpleDeletionPolicy : IDeletionPolicy
    {
        public int PendingDeleteDelaySeconds { get; }
        public int CompletedKeepMinutes { get; }

        public SimpleDeletionPolicy(int pendingDeleteDelaySeconds = 3, int completedKeepMinutes = 1)
        {
            PendingDeleteDelaySeconds = pendingDeleteDelaySeconds;
            CompletedKeepMinutes = completedKeepMinutes;
        }

        public bool ShouldRemove(TaskItem task, DateTime now, bool force)
        {
            if (task.PendingDelete)
            {
                var mark = task.DeleteMarkTime ?? DateTime.MinValue;
                if (force || (now - mark).TotalSeconds >= PendingDeleteDelaySeconds)
                    return true;
            }
            else if (task is { Done: true, CompletedTime: not null } &&
                     (now - task.CompletedTime.Value).TotalMinutes >= CompletedKeepMinutes)
            {
                return true;
            }
            return false;
        }
    }
}