/*
 * 游戏升级提醒 - 系统托盘通知器
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现INotifier接口，使用系统托盘显示通知消息
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Infrastructure.UI
{
    /// <summary>
    /// 使用系统托盘显示通知消息的实现类
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="INotifier"/>接口，
    /// 通过系统托盘显示气泡提示通知用户。
    /// </remarks>
    /// <param name="tray">系统托盘图标控件</param>
    public sealed class TrayNotifier(NotifyIcon tray) : INotifier
    {
        public void Toast(string title, string body, int timeoutMs = 3000)
        {
            tray.BalloonTipTitle = title;
            tray.BalloonTipText = body;
            tray.ShowBalloonTip(timeoutMs);
        }
    }
}