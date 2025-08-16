/*
 * 游戏升级提醒 - 开机自启动管理接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义开机自启动功能的接口，用于管理应用是否随系统启动
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public interface IAutostartManager
    {
        bool IsEnabled();
        void SetEnabled(bool enable);
    }
}