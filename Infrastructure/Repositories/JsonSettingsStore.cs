/*
 * 游戏升级提醒 - JSON设置存储实现
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现ISettingsStore接口，使用JSON文件存储应用设置
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.Infrastructure.Repositories
{
    public sealed class JsonSettingsStore : ISettingsStore
    {
        private static string AppBaseDir => AppContext.BaseDirectory;
        private static string SettingsPath => Path.Combine(AppBaseDir, "settings.json");

        public SettingsData Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new SettingsData();
                var json = File.ReadAllText(SettingsPath, new UTF8Encoding(false));
                return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
            catch
            {
                return new SettingsData();
            }
        }

        public void Save(SettingsData settings)
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, opt), utf8Bom);
            }
            catch
            {
                // ignore
            }
        }
    }
}