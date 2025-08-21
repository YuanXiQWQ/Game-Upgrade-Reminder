/*
 * 游戏升级提醒 - 通知服务接口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义通知服务的接口，用于显示任务完成等提示信息
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    /// <summary>
    /// 定义通知功能的接口，用于向用户显示提示消息
    /// </summary>
    public interface INotifier
    {
        /// <summary>
        /// 显示一个带有标题和内容的通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="body">通知内容</param>
        /// <param name="timeoutMs">通知自动关闭的超时时间（毫秒），默认3000毫秒</param>
        /// <remarks>
        /// 此方法用于向用户显示一个临时的通知消息，通常用于显示操作结果或提醒信息。
        /// 通知会在指定的超时时间后自动消失，或者用户可以手动关闭它。
        /// </remarks>
        void Toast(string title, string body, int timeoutMs = 3000);
    }
}