/*
 * 游戏升级提醒 - 本地化文本排序服务
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 基于当前语言的文本排序服务实现
 * 创建日期: 2025-09-03
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Globalization;
using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.Core.Services
{
    /// <summary>
    /// 本地化文本排序服务，根据当前语言提供适当的文本排序功能
    /// </summary>
    public class LocalizedTextSortingService : ITextSortingService
    {
        private readonly ILocalizationService _localizationService;
        private IComparer<string>? _cachedComparer;
        private string? _cachedLanguage;

        /// <summary>
        /// 初始化本地化文本排序服务
        /// </summary>
        /// <param name="localizationService">本地化服务</param>
        public LocalizedTextSortingService(ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            // 监听语言变更事件，清除缓存的比较器
            _localizationService.LanguageChanged += (_, _) =>
            {
                _cachedComparer = null;
                _cachedLanguage = null;
            };
        }

        /// <summary>
        /// 获取适用于当前语言的字符串比较器
        /// </summary>
        /// <returns>基于当前语言的字符串比较器</returns>
        public IComparer<string> GetStringComparer()
        {
            var currentLanguage = _localizationService.CurrentLanguage;

            // 如果语言没有变化且已有缓存的比较器，直接返回
            if (_cachedComparer != null && _cachedLanguage == currentLanguage)
            {
                return _cachedComparer;
            }

            // 根据当前语言创建适当的比较器
            _cachedComparer = CreateComparerForLanguage(currentLanguage);
            _cachedLanguage = currentLanguage;

            return _cachedComparer;
        }

        /// <summary>
        /// 对字符串列表进行排序
        /// </summary>
        /// <param name="items">要排序的字符串列表</param>
        /// <returns>排序后的字符串列表</returns>
        public IEnumerable<string> SortStrings(IEnumerable<string> items)
        {
            return items.OrderBy(x => x, GetStringComparer());
        }

        /// <summary>
        /// 对字符串列表进行排序（降序）
        /// </summary>
        /// <param name="items">要排序的字符串列表</param>
        /// <returns>降序排序后的字符串列表</returns>
        public IEnumerable<string> SortStringsDescending(IEnumerable<string> items)
        {
            return items.OrderByDescending(x => x, GetStringComparer());
        }

        /// <summary>
        /// 比较两个字符串
        /// </summary>
        /// <param name="x">第一个字符串</param>
        /// <param name="y">第二个字符串</param>
        /// <returns>比较结果：负数表示x小于y，0表示相等，正数表示x大于y</returns>
        public int CompareStrings(string? x, string? y)
        {
            return GetStringComparer().Compare(x, y);
        }

        /// <summary>
        /// 根据语言代码创建适当的字符串比较器
        /// </summary>
        /// <param name="languageCode">语言代码（如 "zh-CN", "en-US"）</param>
        /// <returns>适合该语言的字符串比较器</returns>
        private static IComparer<string> CreateComparerForLanguage(string languageCode)
        {
            try
            {
                // 尝试根据语言代码创建文化信息
                var cultureInfo = CultureInfo.GetCultureInfo(languageCode);
                return StringComparer.Create(cultureInfo, true);
            }
            catch (CultureNotFoundException)
            {
                // 如果语言代码无效，尝试提取主要语言部分
                try
                {
                    var primaryLanguage = languageCode.Split('-')[0];
                    var cultureInfo = CultureInfo.GetCultureInfo(primaryLanguage);
                    return StringComparer.Create(cultureInfo, true);
                }
                catch (CultureNotFoundException)
                {
                    // 如果仍然无效，回退到不区分大小写的序数比较
                    return StringComparer.OrdinalIgnoreCase;
                }
            }
            catch (ArgumentException)
            {
                // 处理其他参数异常，回退到默认比较器
                return StringComparer.OrdinalIgnoreCase;
            }
        }
    }
}