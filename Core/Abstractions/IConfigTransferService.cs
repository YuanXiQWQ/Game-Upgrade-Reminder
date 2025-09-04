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
    /// <summary>
    /// 配置导出操作的状态枚举。
    /// </summary>
    public enum ExportStatus
    {
        /// <summary>导出成功。</summary>
        Success,

        /// <summary>没有可导出的配置文件。</summary>
        Empty,

        /// <summary>导出过程中发生错误。</summary>
        Error
    }

    /// <summary>
    /// 配置导入操作的状态枚举。
    /// </summary>
    public enum ImportStatus
    {
        /// <summary>导入成功。</summary>
        Success,

        /// <summary>ZIP文件中没有有效的配置条目。</summary>
        ZipNoEntries,

        /// <summary>不支持的文件类型。</summary>
        InvalidFileType,

        /// <summary>导入过程中发生错误。</summary>
        Error
    }

    /// <summary>
    /// 单个文件的类型枚举，用于标识导入的配置文件种类。
    /// </summary>
    public enum SingleFileKind
    {
        /// <summary>未指定或ZIP文件。</summary>
        None,

        /// <summary>设置文件 (settings.json)。</summary>
        Settings,

        /// <summary>任务文件 (tasks.json)。</summary>
        Tasks
    }

    /// <summary>
    /// 配置导出操作的结果记录。
    /// </summary>
    /// <param name="Status">导出操作的状态。</param>
    /// <param name="ZipPath">成功导出时生成的ZIP文件路径。</param>
    /// <param name="Error">发生错误时的异常信息。</param>
    public sealed record ExportResult(ExportStatus Status, string? ZipPath = null, Exception? Error = null);

    /// <summary>
    /// 配置导入操作的结果记录。
    /// </summary>
    /// <param name="Status">导入操作的状态。</param>
    /// <param name="ImportedCount">成功导入的文件数量。</param>
    /// <param name="SingleFileKind">单个文件导入时的文件类型。</param>
    /// <param name="Error">发生错误时的异常信息。</param>
    public sealed record ImportResult(
        ImportStatus Status,
        int ImportedCount = 0,
        SingleFileKind SingleFileKind = SingleFileKind.None,
        Exception? Error = null);

    /// <summary>
    /// 配置文件导入导出服务接口，提供配置文件的打包导出和解包导入功能。
    /// </summary>
    public interface IConfigTransferService
    {
        /// <summary>
        /// 导出配置文件到ZIP压缩包。
        /// </summary>
        /// <param name="baseDir">配置文件所在的基础目录。</param>
        /// <returns>导出操作的结果，包含状态、文件路径或错误信息。</returns>
        ExportResult Export(string baseDir);

        /// <summary>
        /// 从文件导入配置（支持ZIP压缩包或单个JSON文件）。
        /// </summary>
        /// <param name="filePath">要导入的文件路径。</param>
        /// <param name="baseDir">配置文件的目标基础目录。</param>
        /// <returns>导入操作的结果，包含状态、导入数量、文件类型或错误信息。</returns>
        ImportResult Import(string filePath, string baseDir);
    }
}