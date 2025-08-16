/*
 * 游戏升级提醒 - 注册表自启动管理
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现IAutostartManager接口，使用Windows注册表管理开机自启动
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;
using System.Windows.Forms;
using Game_Upgrade_Reminder.Core.Abstractions;
using Microsoft.Win32;

namespace Game_Upgrade_Reminder.Infrastructure.System
{
    public sealed class RegistryAutostartManager : IAutostartManager
    {
        private const string AppClass = "Game_Upgrade_Reminder";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                if (key == null) return false;
                var val = key.GetValue(AppClass) as string;
                return !string.IsNullOrEmpty(val);
            }
            catch
            {
                return false;
            }
        }

        public void SetEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                string exe = $"\"{Application.ExecutablePath}\"";
                key.SetValue(AppClass, exe, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(AppClass, throwOnMissingValue: false);
            }
        }
    }
}