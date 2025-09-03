/*
 * 游戏升级提醒 - 配置导入导出抽象定义
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义配置导入导出相关的状态、结果与服务接口
 * 创建日期: 2025-09-03
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public enum ExportStatus { Success, Empty, Error }
    public enum ImportStatus { Success, ZipNoEntries, InvalidFileType, Error }
    public enum SingleFileKind { None, Settings, Tasks }

    public sealed record ExportResult(ExportStatus Status, string? ZipPath = null, Exception? Error = null);
    public sealed record ImportResult(ImportStatus Status, int ImportedCount = 0, SingleFileKind SingleFileKind = SingleFileKind.None, Exception? Error = null);

    public interface IConfigTransferService
    {
        ExportResult Export(string baseDir);
        ImportResult Import(string filePath, string baseDir);
    }
}
