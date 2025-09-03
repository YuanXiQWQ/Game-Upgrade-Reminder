/*
 * 重复任务 - 规格
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-21
 * 最后修改: 2025-09-02
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
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
    /// 提醒后暂停计时，直到用户点击“确认”才开始下一次计时。
    /// </summary>
    public bool PauseUntilDone { get; init; }

    /// <summary>
    /// 是否为有效重复（Mode!=None，且当为 Custom 时 Custom 不是空周期）
    /// </summary>
    [JsonIgnore]
    public bool IsRepeat => Mode switch
    {
        RepeatMode.None => false,
        RepeatMode.Custom => Custom is { IsEmpty: false },
        _ => true
    };

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

    /// <summary>
    /// 是否暂停计时直到用户完成任务
    /// </summary>
    [JsonIgnore]
    public bool IsPauseUntilDone => PauseUntilDone;

    /// <summary>
    /// 提醒后对“下一次发生时间”的偏移量（单位：秒，可正可负，默认 0）。
    /// 例如：-60 表示相比基础周期提前 1 分钟；+120 表示延后 2 分钟。
    /// </summary>
    public int OffsetAfterSeconds { get; init; }
}