/*
 * 游戏升级提醒 - 删除策略接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务删除策略的接口，包括待删除延迟和完成后的保留时间
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 定义任务删除策略的接口，用于控制任务的删除行为
    /// </summary>
    public interface IDeletionPolicy
    {
        /// <summary>
        /// 获取标记为删除后的延迟秒数
        /// </summary>
        /// <remarks>
        /// 默认实现为3秒，与原始实现保持一致。
        /// 这表示任务被标记为删除后，将在指定秒数后执行实际删除操作。
        /// </remarks>
        int PendingDeleteDelaySeconds { get; }

        /// <summary>
        /// 获取任务完成后保留的秒数
        /// </summary>
        /// <remarks>
        /// 默认实现为60秒（1分钟），用于控制任务完成后延迟删除的时间。
        /// </remarks>
        int CompletedKeepSeconds { get; }

        /// <summary>
        /// 确定是否应该删除指定的任务
        /// </summary>
        /// <param name="task">要检查的任务</param>
        /// <param name="now">当前时间</param>
        /// <param name="force">是否强制删除，忽略时间限制</param>
        /// <returns>如果应该删除任务则返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法根据任务的当前状态、时间限制和force参数来决定是否应该删除任务。
        /// 如果force为true，则忽略时间限制，立即删除任务。
        /// 否则，将根据PendingDeleteDelaySeconds和CompletedKeepSeconds属性决定是否删除。
        /// </remarks>
        bool ShouldRemove(TaskItem task, DateTime now, bool force);
    }
}