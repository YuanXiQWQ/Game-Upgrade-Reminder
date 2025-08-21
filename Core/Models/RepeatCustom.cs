/*
 * 重复任务 - 自定义周期
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
/// 自定义重复周期，允许精确到 年/月/日/时/分/秒。
/// 至少应当有一个字段 &gt; 0 才表示有效周期。
/// </summary>
public sealed class RepeatCustom
{
    /// <summary>年数（≥0）</summary>
    public int Years { get; init; }

    /// <summary>月数（≥0）</summary>
    public int Months { get; init; }

    /// <summary>天数（≥0）</summary>
    public int Days { get; init; }

    /// <summary>小时数（≥0）</summary>
    public int Hours { get; init; }

    /// <summary>分钟数（≥0）</summary>
    public int Minutes { get; init; }

    /// <summary>秒数（≥0）</summary>
    public int Seconds { get; init; }

    /// <summary>
    /// 是否为空周期（6项均为 0）
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Years == 0 && Months == 0 && Days == 0 && Hours == 0 && Minutes == 0 && Seconds == 0;
}