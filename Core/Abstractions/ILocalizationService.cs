/*
 * 游戏升级提醒 - 本地化服务接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义多语言本地化服务的接口
 * 创建日期: 2025-08-22
 * 最后修改: 2025-08-24
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 本地化服务接口，提供多语言文本获取功能
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// 获取当前语言代码
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// 获取所有可用的语言列表
        /// </summary>
        IReadOnlyList<string> AvailableLanguages { get; }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码（如 "zh-CN", "en-US"）</param>
        /// <returns>是否设置成功</returns>
        bool SetLanguage(string languageCode);

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">文本键</param>
        /// <param name="defaultValue">默认值（当找不到对应文本时返回）</param>
        /// <returns>本地化后的文本</returns>
        string GetText(string key, string? defaultValue = null);

        /// <summary>
        /// 获取格式化的本地化文本
        /// </summary>
        /// <param name="key">文本键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的本地化文本</returns>
        string GetFormattedText(string key, params object[] args);

        /// <summary>
        /// 语言变更事件
        /// </summary>
        event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
    }

    /// <summary>
    /// 语言变更事件参数
    /// </summary>
    public class LanguageChangedEventArgs(string oldLanguage, string newLanguage) : EventArgs
    {
        /// <summary>
        /// 获取变更前的语言代码
        /// </summary>
        public string OldLanguage { get; } = oldLanguage;

        /// <summary>
        /// 获取变更后的语言代码
        /// </summary>
        public string NewLanguage { get; } = newLanguage;
    }
}