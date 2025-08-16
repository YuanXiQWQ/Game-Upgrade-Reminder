/*
 * 游戏升级提醒 - 删除策略接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务删除策略的接口，包括待删除延迟和完成后的保留时间
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public interface IDeletionPolicy
    {
        /// <summary>标记删除后延迟秒数（与原实现一致：3秒）。</summary>
        int PendingDeleteDelaySeconds { get; }

        /// <summary>完成后保留分钟数（与原实现一致：1分钟）。</summary>
        int CompletedKeepMinutes { get; }

        /// <summary>根据当前时间与force判断是否应彻底删除该任务。</summary>
        bool ShouldRemove(TaskItem task, DateTime now, bool force);
    }
}