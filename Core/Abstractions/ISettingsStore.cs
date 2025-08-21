/*
 * 游戏升级提醒 - 设置存储接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义设置数据存储的接口，用于加载和保存应用程序设置
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
    /// 定义设置存储功能的接口，用于加载和保存应用程序设置
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>
        /// 从持久化存储中加载设置
        /// </summary>
        /// <returns>包含应用程序设置的<see cref="SettingsData"/>对象</returns>
        /// <remarks>
        /// 如果找不到保存的设置，应返回一个具有默认值的<see cref="SettingsData"/>实例。
        /// 实现应处理所有可能的I/O异常，并记录任何错误。
        /// </remarks>
        SettingsData Load();

        /// <summary>
        /// 将设置保存到持久化存储中
        /// </summary>
        /// <param name="settings">要保存的<see cref="SettingsData"/>对象</param>
        /// <remarks>
        /// 实现应确保设置的原子性保存，并处理所有可能的I/O异常。
        /// 如果保存操作失败，应记录错误并可能抛出异常。
        /// </remarks>
        void Save(SettingsData settings);
    }
}