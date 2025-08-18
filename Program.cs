/*
 * 游戏升级提醒 - 主程序入口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 游戏升级提醒工具，用于跟踪和管理游戏中的升级进度
 * 创建日期: 2025-08-13
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder
{
    /// <summary>
    /// 应用程序入口点。
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 主入口方法。
        /// 支持命令行参数 <c>--minimized</c>：以最小化方式启动主窗口（并立即隐藏）。
        /// </summary>
        /// <param name="args">命令行参数。</param>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var startMinimized = args is { Length: > 0 } &&
                                 string.Equals(args[0], "--minimized", StringComparison.OrdinalIgnoreCase);

            var mainForm = new UI.MainForm();
            if (startMinimized)
            {
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.Hide();
            }

            Application.Run(mainForm);
        }
    }
}