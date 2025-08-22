/*
 * 重复任务 - 跳过提醒规则
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-21
 * 最后修改: 2025-08-21
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Text.Json.Serialization;

namespace Game_Upgrade_Reminder.Core.Models;

/// <summary>
/// 跳过提醒规则：每提醒 <see cref="RemindTimes"/> 次后，跳过 <see cref="SkipTimes"/> 次提醒。
/// </summary>
public sealed class SkipRule
{
    /// <summary>
    /// 每提醒 N 次（≥0）
    /// </summary>
    public int RemindTimes { get; init; }

    /// <summary>
    /// 跳过 M 次（≥0）
    /// </summary>
    public int SkipTimes { get; init; }

    /// <summary>
    /// 是否启用（当 N>0 且 M>0 时认为启用）
    /// </summary>
    [JsonIgnore]
    public bool IsActive => RemindTimes > 0 && SkipTimes > 0;
}