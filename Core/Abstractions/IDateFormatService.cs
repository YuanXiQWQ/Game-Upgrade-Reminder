/*
 * 游戏升级提醒 - 日期格式服务接口
 * 负责根据当前语言环境生成 DateTimePicker 的格式字符串，并提供统一的日期/时间格式化方法。
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-24
 * 最后修改: 2025-08-24
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Abstractions
{
    public interface IDateFormatService
    {
        // 返回仅日期的 DateTimePicker 自定义格式（例如：yyyy-MM-dd / dd-MM-yyyy / MM-dd-yyyy）
        string GetDatePickerDateFormat();

        // 返回日期+时间的 DateTimePicker 自定义格式（例如：yyyy-MM-dd HH:mm / dd-MM-yyyy HH:mm / MM-dd-yyyy HH:mm）
        string GetDatePickerDateTimeFormat();

        // 将日期部分格式化（根据语言顺序；includeYear=false 时不含年份）
        string FormatDate(DateTime dt, bool includeYear);

        // 将时间部分格式化（24 小时制；includeSeconds 决定是否包含秒）
        string FormatTime(DateTime dt, bool includeSeconds);

        // 按本地化顺序拼接日期+时间
        string FormatDateTime(DateTime dt, bool includeYear, bool includeSeconds);

        // 相对当前时间的人类友好显示：同日仅时间、同年不含年份、跨年含年份
        string FormatSmartDateTime(DateTime dt, DateTime now);
    }
}