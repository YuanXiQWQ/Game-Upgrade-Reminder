/*
 * 游戏升级提醒 - 设置数据模型
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义应用程序设置的数据结构
 * 创建日期: 2025-08-14
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Collections.Generic;

namespace Game_Upgrade_Reminder.Models
{
    public class SettingsData
    {
        // 界面设置
        public string UiFontFamily { get; set; } = "Microsoft YaHei UI";
        public float UiFontSize { get; set; } = 9f;
        public bool UiFontBold { get; set; }
        public bool UiFontItalic { get; set; }
        public bool TopMost { get; init; } = true;

        // 通知设置
        public bool NotifyOnCompletion { get; init; } = true;
        public int NotifyBeforeMinutes { get; init; } = 5;

        // 启动设置
        public bool AutoStartWithWindows { get; init; }
        public bool AutoStartMinimized { get; init; }
        public bool StartupOnBoot { get; set; }
        public bool MinimizeOnClose { get; set; } = true;

        // 预设
        public List<string> AccountPresets { get; init; } = new() { "DefaultAccount" };
        public List<string> TaskPresets { get; set; } = new() { "升级", "维护" };
        public List<string> Accounts { get; set; } = new();

        // 字体属性（用于向后兼容）
        [System.Text.Json.Serialization.JsonIgnore]
        public FontSpec UiFont
        {
            get => new() { Family = UiFontFamily, Size = UiFontSize, Bold = UiFontBold, Italic = UiFontItalic };
            set
            {
                UiFontFamily = value.Family;
                UiFontSize = value.Size > 0 ? value.Size : 9f;
                UiFontBold = value.Bold;
                UiFontItalic = value.Italic;
            }
        }
    }
}