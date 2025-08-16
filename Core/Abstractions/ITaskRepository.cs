/*
 * 游戏升级提醒 - 任务存储库接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义任务数据的存储接口，用于加载和保存任务列表
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 定义任务数据持久化的接口，负责任务的加载和保存
    /// </summary>
    public interface ITaskRepository
    {
        /// <summary>
        /// 从持久化存储中加载所有任务
        /// </summary>
        /// <returns>包含所有任务的<see cref="List{TaskItem}"/>集合</returns>
        /// <remarks>
        /// 如果存储中没有任何任务或发生错误，应返回空列表而不是null。
        /// 实现应处理所有可能的I/O异常，并记录任何错误。
        /// </remarks>
        List<TaskItem> Load();

        /// <summary>
        /// 将任务集合保存到持久化存储中
        /// </summary>
        /// <param name="tasks">要保存的任务集合</param>
        /// <remarks>
        /// 此方法会覆盖存储中的所有现有任务。
        /// 实现应确保数据的原子性保存，并处理所有可能的I/O异常。
        /// 如果保存操作失败，应记录错误并可能抛出异常。
        /// </remarks>
        void Save(IEnumerable<TaskItem> tasks);
    }
}