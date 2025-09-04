/*
 * 游戏升级提醒 - 配置导入导出服务实现
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 实现配置文件的打包导出与解包导入逻辑
 * 创建日期: 2025-09-03
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.IO.Compression;
using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 配置传输服务实现类，提供配置文件的导出和导入功能
    /// </summary>
    public sealed class ConfigTransferService : IConfigTransferService
    {
        /// <summary>
        /// 导出配置文件到ZIP压缩包
        /// </summary>
        /// <param name="baseDir">基础目录路径，包含settings.json和tasks.json文件</param>
        /// <returns>导出操作结果，包含状态信息和ZIP文件路径</returns>
        public ExportResult Export(string baseDir)
        {
            try
            {
                // 构建配置文件的完整路径
                var settingsPath = Path.Combine(baseDir, "settings.json");
                var tasksPath = Path.Combine(baseDir, "tasks.json");
                var zipPath = Path.Combine(baseDir, "config.zip");

                // 如果目标ZIP文件已存在，先删除它
                if (File.Exists(zipPath))
                {
                    try
                    {
                        File.Delete(zipPath);
                    }
                    catch
                    {
                        // 忽略
                    }
                }

                // 创建ZIP文件并添加配置文件
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                var added = 0; // 记录成功添加的文件数量

                // 添加设置文件（如果存在）
                if (File.Exists(settingsPath))
                {
                    zip.CreateEntryFromFile(settingsPath, "settings.json", CompressionLevel.Optimal);
                    added++;
                }

                // 检查任务文件是否存在，如果不存在则提前返回
                if (!File.Exists(tasksPath))
                    return added == 0
                        ? new ExportResult(ExportStatus.Empty) // 没有任何文件可导出
                        : new ExportResult(ExportStatus.Success, ZipPath: zipPath); // 只有设置文件

                // 添加任务文件
                zip.CreateEntryFromFile(tasksPath, "tasks.json", CompressionLevel.Optimal);

                return new ExportResult(ExportStatus.Success, ZipPath: zipPath);
            }
            catch (Exception ex)
            {
                // 捕获所有异常并返回错误结果
                return new ExportResult(ExportStatus.Error, Error: ex);
            }
        }

        /// <summary>
        /// 从文件导入配置
        /// </summary>
        /// <param name="filePath">要导入的文件路径（支持ZIP文件或单个JSON文件）</param>
        /// <param name="baseDir">目标基础目录路径</param>
        /// <returns>导入操作结果，包含状态信息和导入的文件数量</returns>
        public ImportResult Import(string filePath, string baseDir)
        {
            try
            {
                // 构建目标配置文件的完整路径
                var settingsPath = Path.Combine(baseDir, "settings.json");
                var tasksPath = Path.Combine(baseDir, "tasks.json");

                // 根据文件扩展名判断导入类型
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                // 处理ZIP文件导入
                if (ext == ".zip")
                {
                    var imported = 0; // 记录成功导入的文件数量
                    using var zip = ZipFile.OpenRead(filePath);

                    // 遍历ZIP文件中的所有条目
                    foreach (var entry in zip.Entries)
                    {
                        var name = Path.GetFileName(entry.FullName);
                        // 检查并提取设置文件
                        if (string.Equals(name, "settings.json", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ExtractToFile(settingsPath, overwrite: true);
                            imported++;
                        }
                        // 检查并提取任务文件
                        else if (string.Equals(name, "tasks.json", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ExtractToFile(tasksPath, overwrite: true);
                            imported++;
                        }
                    }

                    // 返回ZIP导入结果
                    return imported == 0
                        ? new ImportResult(ImportStatus.ZipNoEntries) // ZIP文件中没有有效的配置文件
                        : new ImportResult(ImportStatus.Success, ImportedCount: imported);
                }

                // 处理单个JSON文件导入
                var fileName = Path.GetFileName(filePath);
                // 导入设置文件
                if (string.Equals(fileName, "settings.json", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, settingsPath, overwrite: true);
                    return new ImportResult(ImportStatus.Success, SingleFileKind: SingleFileKind.Settings);
                }

                // 导入任务文件
                if (!string.Equals(fileName, "tasks.json", StringComparison.OrdinalIgnoreCase))
                    return new ImportResult(ImportStatus.InvalidFileType);

                File.Copy(filePath, tasksPath, overwrite: true);
                return new ImportResult(ImportStatus.Success, SingleFileKind: SingleFileKind.Tasks);
            }
            catch (Exception ex)
            {
                // 捕获所有异常并返回错误结果
                return new ImportResult(ImportStatus.Error, Error: ex);
            }
        }
    }
}