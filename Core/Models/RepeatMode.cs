/*
 * 重复任务 - 模式枚举
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
