/*
 * 游戏升级提醒 - JSON设置存储实现
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现ISettingsStore接口，使用JSON文件存储应用设置
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Text;
using System.Text.Json;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.Infrastructure.Repositories
{
    /// <summary>
    /// 使用JSON文件实现设置存储
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="ISettingsStore"/>接口，
    /// 使用JSON格式将设置数据持久化到应用程序目录下的settings.json文件中。
    /// 如果读取或写入过程中发生错误，将静默失败并返回默认值。
    /// </remarks>
    public sealed class JsonSettingsStore : ISettingsStore
    {
        /// <summary>
        /// 获取应用程序基础目录
        /// </summary>
        private static string AppBaseDir => AppContext.BaseDirectory;

        /// <summary>
        /// 获取设置文件的完整路径
        /// </summary>
        private static string SettingsPath => Path.Combine(AppBaseDir, "settings.json");

        private static readonly JsonSerializerOptions SJsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// 从JSON文件加载设置
        /// </summary>
        /// <returns>加载的设置数据，如果文件不存在或反序列化失败则返回新的<see cref="SettingsData"/>实例</returns>
        /// <remarks>
        /// 此方法会：
        /// 1. 检查设置文件是否存在，如果不存在则返回新的<see cref="SettingsData"/>实例
        /// 2. 读取文件内容并使用UTF-8编码（不带BOM）
        /// 3. 将JSON反序列化为<see cref="SettingsData"/>对象
        /// 4. 如果任何步骤失败，返回新的<see cref="SettingsData"/>实例
        /// </remarks>
        public SettingsData Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new SettingsData();
                var json = File.ReadAllText(SettingsPath, new UTF8Encoding(false));
                return JsonSerializer.Deserialize<SettingsData>(json, SJsonOptions) ?? new SettingsData();
            }
            catch
            {
                return new SettingsData();
            }
        }

        /// <summary>
        /// 将设置保存到JSON文件
        /// </summary>
        /// <param name="settings">要保存的设置数据</param>
        /// <remarks>
        /// 此方法会：
        /// 1. 使用带缩进的JSON格式
        /// 2. 使用带BOM的UTF-8编码保存文件
        /// 3. 如果保存过程中发生错误，将静默失败
        /// </remarks>
        public void Save(SettingsData settings)
        {
            try
            {
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SJsonOptions), utf8Bom);
            }
            catch
            {
                // 静默失败，不抛出异常
            }
        }
    }
}