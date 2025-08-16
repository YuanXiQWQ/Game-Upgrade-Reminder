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

using Game_Upgrade_Reminder.Core.Abstractions;
using Microsoft.Win32;

namespace Game_Upgrade_Reminder.Infrastructure.System
{
    /// <summary>
    /// 使用Windows注册表实现应用程序自启动管理
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="IAutostartManager"/>接口，
    /// 通过操作Windows注册表中的运行项(Run)来控制应用程序是否随系统启动。
    /// 设置保存在HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run下。
    /// </remarks>
    public sealed class RegistryAutostartManager : IAutostartManager
    {
        /// <summary>
        /// 应用程序在注册表中的标识名称
        /// </summary>
        private const string AppClass = "Game_Upgrade_Reminder";

        /// <summary>
        /// Windows注册表中存储自启动项的路径
        /// </summary>
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 检查应用程序是否已设置为自动启动
        /// </summary>
        /// <returns>如果已启用自启动则返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法会：
        /// 1. 打开当前用户的Run注册表项
        /// 2. 检查是否存在以<see cref="AppClass"/>命名的值
        /// 3. 如果注册表项不存在或访问出错，返回false
        /// </remarks>
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

        /// <summary>
        /// 启用或禁用应用程序的自启动
        /// </summary>
        /// <param name="enable">true启用自启动，false禁用自启动</param>
        /// <remarks>
        /// 当enable为true时：
        /// 1. 在注册表Run项下创建一个新值，名称为<see cref="AppClass"/>
        /// 2. 值为当前可执行文件的完整路径，包含引号
        ///
        /// 当enable为false时：
        /// 1. 从注册表Run项中删除对应的值
        ///
        /// 注意：此操作需要管理员权限才能成功修改注册表
        /// </remarks>
        public void SetEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exe = $"\"{Application.ExecutablePath}\"";
                key.SetValue(AppClass, exe, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(AppClass, throwOnMissingValue: false);
            }
        }
    }
}