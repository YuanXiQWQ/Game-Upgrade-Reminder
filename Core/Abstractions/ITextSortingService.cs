/*
 * 游戏升级提醒 - 文本排序服务接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义基于当前语言的文本排序服务接口
 * 创建日期: 2025-09-03
 * 最后修改: 2025-09-03
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 文本排序服务接口，提供基于当前语言的文本排序功能
    /// </summary>
    public interface ITextSortingService
    {
        /// <summary>
        /// 获取适用于当前语言的字符串比较器
        /// </summary>
        /// <returns>基于当前语言的字符串比较器</returns>
        IComparer<string> GetStringComparer();

        /// <summary>
        /// 对字符串列表进行排序
        /// </summary>
        /// <param name="items">要排序的字符串列表</param>
        /// <returns>排序后的字符串列表</returns>
        IEnumerable<string> SortStrings(IEnumerable<string> items);

        /// <summary>
        /// 对字符串列表进行排序（降序）
        /// </summary>
        /// <param name="items">要排序的字符串列表</param>
        /// <returns>降序排序后的字符串列表</returns>
        IEnumerable<string> SortStringsDescending(IEnumerable<string> items);

        /// <summary>
        /// 比较两个字符串
        /// </summary>
        /// <param name="x">第一个字符串</param>
        /// <param name="y">第二个字符串</param>
        /// <returns>比较结果：负数表示x小于y，0表示相等，正数表示x大于y</returns>
        int CompareStrings(string? x, string? y);
    }
}