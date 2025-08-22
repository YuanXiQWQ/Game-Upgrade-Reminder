/*
 * 重复任务 - 模式枚举
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-21
 * 最后修改: 2025-08-21
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Models;

/// <summary>
/// 重复模式
/// </summary>
public enum RepeatMode
{
    /// <summary>
    /// 不重复
    /// </summary>
    None = 0,

    /// <summary>
    /// 每天
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 每周
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// 每月
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// 每年
    /// </summary>
    Yearly = 4,

    /// <summary>
    /// 自定义（由 RepeatCustom 指定）
    /// </summary>
    Custom = 5
}