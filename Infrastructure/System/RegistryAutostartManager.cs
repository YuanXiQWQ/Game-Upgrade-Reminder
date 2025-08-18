/*
 * 游戏升级提醒 - 基于注册表的自启动管理器
 * 作者: YuanXiQWQ
 * 项目: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 使用Windows注册表实现IAutostartManager接口来控制程序自启动
 * 创建于: 2025-08-15
 * 最后修改: 2025-08-16
 *
 * 许可证: GNU Affero通用公共许可证 v3.0 (AGPL-3.0)
 * https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;
using Microsoft.Win32;

namespace Game_Upgrade_Reminder.Infrastructure.System
{
    /// <summary>
    /// 通过Windows注册表实现应用程序自启动管理。
    /// </summary>
    /// <remarks>
    /// 值存储在以下位置：
    /// HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
    /// </remarks>
    public sealed class RegistryAutostartManager : IAutostartManager
    {
        /// <summary>
        /// 在Run键中用于此应用程序的值名称。
        /// </summary>
        private const string AppClass = "Game_Upgrade_Reminder";

        /// <summary>
        /// 当前用户的Run键路径。
        /// </summary>
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 检查应用程序是否配置为在用户登录时运行。
        /// </summary>
        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                if (key == null) return false;

                var value = key.GetValue(AppClass) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 为当前用户启用或禁用自启动。
        /// 启用时，默认添加"--minimized"命令行参数。
        /// </summary>
        public void SetEnabled(bool enable)
        {
            try
            {
                // 确保Run键存在，即使之前不存在
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

                if (enable)
                {
                    // 为路径添加引号，防止路径中包含空格导致问题
                    var exe = $"\"{Application.ExecutablePath}\"";

                    // 添加最小化标志，以便Program.cs可以检测到
                    var commandLine = exe + " --minimized";

                    // 写入自启动值
                    key.SetValue(AppClass, commandLine, RegistryValueKind.String);
                }
                else
                {
                    // 禁用自启动时删除对应的值
                    key.DeleteValue(AppClass, throwOnMissingValue: false);
                }
            }
            catch
            {
                // 典型失败原因：安全软件阻止了注册表写入
                // 并不典型但十分有可能的失败原因：我又写出了一个bug
            }
        }
    }
}