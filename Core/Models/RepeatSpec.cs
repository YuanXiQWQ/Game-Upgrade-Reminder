/*
 * 重复任务 - 规格
 */

using System.Text.Json.Serialization;

namespace Game_Upgrade_Reminder.Core.Models;

/// <summary>
/// 重复任务设置。
/// </summary>
public sealed class RepeatSpec
{
    /// <summary>
    /// 重复模式（默认 None）
    /// </summary>
    public RepeatMode Mode { get; init; } = RepeatMode.None;

    /// <summary>
    /// 自定义重复周期（当 Mode=Custom 时有效）
    /// </summary>
    public RepeatCustom? Custom { get; init; }

    /// <summary>
    /// 结束时间（可空）。达到该时刻后不再提醒。
    /// </summary>
    public DateTime? EndAt { get; init; }

    /// <summary>
    /// 跳过提醒规则（可空）。
    /// </summary>
    public SkipRule? Skip { get; init; }

    /// <summary>
    /// 是否为有效重复（Mode!=None，且当为 Custom 时 Custom 不是空周期）
    /// </summary>
    [JsonIgnore]
    public bool IsRepeat => Mode != RepeatMode.None && (Mode != RepeatMode.Custom || (Custom != null && !Custom.IsEmpty));

    /// <summary>
    /// 是否设置了结束时间
    /// </summary>
    [JsonIgnore]
    public bool HasEnd => EndAt.HasValue;

    /// <summary>
    /// 是否设置了跳过规则
    /// </summary>
    [JsonIgnore]
    public bool HasSkip => Skip?.IsActive == true;
}
