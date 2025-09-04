/*
 * 游戏升级提醒 - JSON任务存储实现
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现ITaskRepository接口，使用JSON文件存储任务数据
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
    /// 使用JSON文件实现任务存储
    /// </summary>
    /// <remarks>
    /// 此类实现了<see cref="ITaskRepository"/>接口，
    /// 使用JSON格式将任务数据持久化到应用程序目录下的tasks.json文件中。
    /// 如果读取或写入过程中发生错误，将静默失败并返回空列表。
    /// </remarks>
    public sealed class JsonTaskRepository : ITaskRepository
    {
        /// <summary>
        /// 获取应用程序基础目录
        /// </summary>
        private static string AppBaseDir => AppContext.BaseDirectory;

        /// <summary>
        /// 获取任务文件的完整路径
        /// </summary>
        private static string TasksPath => Path.Combine(AppBaseDir, "tasks.json");

        private static readonly JsonSerializerOptions SJsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// 从JSON文件加载任务列表
        /// </summary>
        /// <returns>加载的任务列表，如果文件不存在或反序列化失败则返回空列表</returns>
        /// <remarks>
        /// 此方法会：
        /// 1. 检查任务文件是否存在，如果不存在则返回空列表
        /// 2. 读取文件内容并使用UTF-8编码（不带BOM）
        /// 3. 将JSON反序列化为<see cref="List{TaskItem}"/>对象
        /// 4. 如果任何步骤失败，返回空列表
        /// </remarks>
        public List<TaskItem> Load()
        {
            try
            {
                if (!File.Exists(TasksPath)) return [];
                var json = File.ReadAllText(TasksPath, new UTF8Encoding(false));
                return JsonSerializer.Deserialize<List<TaskItem>>(json, SJsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// 将任务列表保存到JSON文件
        /// </summary>
        /// <param name="tasks">要保存的任务列表</param>
        /// <remarks>
        /// 此方法会：
        /// 1. 使用带缩进的JSON格式
        /// 2. 使用带BOM的UTF-8编码保存文件
        /// 3. 如果保存过程中发生错误，将静默失败
        /// 注意：此方法会覆盖现有的任务文件
        /// </remarks>
        public void Save(IEnumerable<TaskItem> tasks)
        {
            try
            {
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(TasksPath, JsonSerializer.Serialize(tasks, SJsonOptions), utf8Bom);
            }
            catch
            {
                // 忽略
            }
        }
    }
}