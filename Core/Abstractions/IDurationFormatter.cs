/*
 * 游戏升级提醒 - 时长格式化接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义时长格式化的接口，用于将时间间隔格式化为可读的字符串
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 用于“持续时间”列的字符串格式化（与原 MainForm.FormatTime 输出严格一致）。
    /// </summary>
    public interface IDurationFormatter
    {
        string Format(int days, int hours, int minutes, int seconds = 0, bool showSeconds = false);
    }
}