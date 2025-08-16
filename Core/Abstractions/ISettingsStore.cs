/*
 * 游戏升级提醒 - 设置存储接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义设置数据存储的接口，用于加载和保存应用程序设置
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public interface ISettingsStore
    {
        SettingsData Load();
        void Save(SettingsData settings);
    }
}