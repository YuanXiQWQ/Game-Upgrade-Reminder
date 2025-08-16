/*
 * 游戏升级提醒 - 系统托盘通知器
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现INotifier接口，使用系统托盘显示通知消息
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Windows.Forms;
using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Infrastructure.UI
{
    public sealed class TrayNotifier : INotifier
    {
        private readonly NotifyIcon _tray;

        public TrayNotifier(NotifyIcon tray)
        {
            _tray = tray;
        }

        public void Toast(string title, string body, int timeoutMs = 3000)
        {
            _tray.BalloonTipTitle = title;
            _tray.BalloonTipText  = body;
            _tray.ShowBalloonTip(timeoutMs);
        }
    }
}