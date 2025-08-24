/*
 * 游戏升级提醒 - JSON本地化服务
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 基于JSON文件的本地化服务实现
 * 创建日期: 2025-08-22
 * 最后修改: 2025-08-24
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Text.Json;
using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 基于JSON文件的本地化服务实现
    /// </summary>
    public sealed class JsonLocalizationService : ILocalizationService
    {
        private readonly string _localizationDirectory;
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
        private string _currentLanguage = "zh-CN";

        /// <summary>
        /// 初始化本地化服务
        /// </summary>
        /// <param name="localizationDirectory">本地化文件目录路径</param>
        public JsonLocalizationService(string? localizationDirectory = null)
        {
            _localizationDirectory = localizationDirectory ?? 
                Path.Combine(AppContext.BaseDirectory, "Localization");
            
            LoadAvailableLanguages();
            LoadLanguage(_currentLanguage);
        }

        /// <summary>
        /// 获取当前语言代码
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 获取所有可用的语言列表
        /// </summary>
        public IReadOnlyList<string> AvailableLanguages { get; private set; } = [];

        /// <summary>
        /// 语言变更事件
        /// </summary>
        public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否设置成功</returns>
        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode) || languageCode == _currentLanguage)
                return false;

            if (!AvailableLanguages.Contains(languageCode))
                return false;

            var oldLanguage = _currentLanguage;
            if (LoadLanguage(languageCode))
            {
                _currentLanguage = languageCode;
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, _currentLanguage));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">文本键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>本地化后的文本</returns>
        public string GetText(string key, string? defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValue ?? key;

            if (_translations.TryGetValue(_currentLanguage, out var languageDict) &&
                languageDict.TryGetValue(key, out var text))
            {
                return text;
            }

            // 如果当前语言没有找到，尝试使用中文作为后备
            if (_currentLanguage != "zh-CN" && 
                _translations.TryGetValue("zh-CN", out var fallbackDict) &&
                fallbackDict.TryGetValue(key, out var fallbackText))
            {
                return fallbackText;
            }

            return defaultValue ?? key;
        }

        /// <summary>
        /// 获取格式化的本地化文本
        /// </summary>
        /// <param name="key">文本键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的本地化文本</returns>
        public string GetFormattedText(string key, params object[] args)
        {
            var template = GetText(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// 加载可用语言列表
        /// </summary>
        private void LoadAvailableLanguages()
        {
            var languages = new List<string>();

            if (Directory.Exists(_localizationDirectory))
            {
                var jsonFiles = Directory.GetFiles(_localizationDirectory, "*.json");
                foreach (var file in jsonFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        languages.Add(fileName);
                    }
                }
            }

            // 确保至少有中文
            if (!languages.Contains("zh-CN"))
            {
                languages.Add("zh-CN");
            }

            AvailableLanguages = languages.AsReadOnly();
        }

        /// <summary>
        /// 加载指定语言的翻译文件
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否加载成功</returns>
        private bool LoadLanguage(string languageCode)
        {
            try
            {
                var filePath = Path.Combine(_localizationDirectory, $"{languageCode}.json");
                
                if (!File.Exists(filePath))
                {
                    // 如果文件不存在，创建一个空翻译字典
                    _translations[languageCode] = new Dictionary<string, string>();
                    return true;
                }

                var jsonContent = File.ReadAllText(filePath);
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                
                if (translations != null)
                {
                    _translations[languageCode] = translations;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载语言文件 {languageCode} 时出错: {ex.Message}");
            }

            return false;
        }
    }
}
