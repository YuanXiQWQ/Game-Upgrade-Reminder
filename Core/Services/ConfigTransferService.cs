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
    public sealed class ConfigTransferService : IConfigTransferService
    {
        public ExportResult Export(string baseDir)
        {
            try
            {
                var settingsPath = Path.Combine(baseDir, "settings.json");
                var tasksPath = Path.Combine(baseDir, "tasks.json");
                var zipPath = Path.Combine(baseDir, "config.zip");

                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); } catch { /* ignore */ }
                }

                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                var added = 0;
                if (File.Exists(settingsPath))
                {
                    zip.CreateEntryFromFile(settingsPath, "settings.json", CompressionLevel.Optimal);
                    added++;
                }
                if (File.Exists(tasksPath))
                {
                    zip.CreateEntryFromFile(tasksPath, "tasks.json", CompressionLevel.Optimal);
                    added++;
                }

                return added == 0
                    ? new ExportResult(ExportStatus.Empty)
                    : new ExportResult(ExportStatus.Success, ZipPath: zipPath);
            }
            catch (Exception ex)
            {
                return new ExportResult(ExportStatus.Error, Error: ex);
            }
        }

        public ImportResult Import(string filePath, string baseDir)
        {
            try
            {
                var settingsPath = Path.Combine(baseDir, "settings.json");
                var tasksPath = Path.Combine(baseDir, "tasks.json");

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".zip")
                {
                    int imported = 0;
                    using var zip = ZipFile.OpenRead(filePath);
                    foreach (var entry in zip.Entries)
                    {
                        var name = Path.GetFileName(entry.FullName);
                        if (string.Equals(name, "settings.json", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ExtractToFile(settingsPath, overwrite: true);
                            imported++;
                        }
                        else if (string.Equals(name, "tasks.json", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ExtractToFile(tasksPath, overwrite: true);
                            imported++;
                        }
                    }

                    return imported == 0
                        ? new ImportResult(ImportStatus.ZipNoEntries)
                        : new ImportResult(ImportStatus.Success, ImportedCount: imported);
                }

                // single JSON file
                var fileName = Path.GetFileName(filePath);
                if (string.Equals(fileName, "settings.json", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, settingsPath, overwrite: true);
                    return new ImportResult(ImportStatus.Success, SingleFileKind: SingleFileKind.Settings);
                }
                if (string.Equals(fileName, "tasks.json", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, tasksPath, overwrite: true);
                    return new ImportResult(ImportStatus.Success, SingleFileKind: SingleFileKind.Tasks);
                }

                return new ImportResult(ImportStatus.InvalidFileType);
            }
            catch (Exception ex)
            {
                return new ImportResult(ImportStatus.Error, Error: ex);
            }
        }
    }
}
