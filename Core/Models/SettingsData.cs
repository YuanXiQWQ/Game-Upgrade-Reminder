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

namespace Game_Upgrade_Reminder.Core.Models
{
    /// <summary>
    /// 表示应用程序的设置数据，包含所有可配置的应用程序设置
    /// </summary>
    public class SettingsData
    {
        // 界面设置
        /// <summary>
        /// 获取或设置界面字体家族名称
        /// </summary>
        /// <value>默认为“微软雅黑 UI"</value>
        public string UiFontFamily { get; set; } = "Microsoft YaHei UI";

        /// <summary>
        /// 获取或设置界面字体大小（以磅为单位）
        /// </summary>
        /// <value>默认值为9.0f</value>
        public float UiFontSize { get; set; } = 9f;

        /// <summary>
        /// 获取或设置一个值，指示界面字体是否为粗体
        /// </summary>
        public bool UiFontBold { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示界面字体是否为斜体
        /// </summary>
        public bool UiFontItalic { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示主窗口是否始终置顶
        /// </summary>
        /// <value>默认为true</value>
        public bool TopMost { get; init; } = true;

        // 通知设置
        /// <summary>
        /// 获取或设置一个值，指示任务完成时是否显示通知
        /// </summary>
        /// <value>默认为true</value>
        public bool NotifyOnCompletion { get; init; } = true;

        /// <summary>
        /// 获取或设置任务到期前多少分钟显示提醒
        /// </summary>
        /// <value>默认值为5分钟</value>
        public int NotifyBeforeMinutes { get; init; } = 5;

        // 启动设置
        /// <summary>
        /// 获取或设置一个值，指示是否随Windows自动启动
        /// </summary>
        public bool AutoStartWithWindows { get; init; }

        /// <summary>
        /// 获取或设置一个值，指示自动启动时是否最小化窗口
        /// </summary>
        public bool AutoStartMinimized { get; init; }

        /// <summary>
        /// 获取或设置一个值，指示系统启动时是否自动启动应用程序
        /// </summary>
        public bool StartupOnBoot { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示点击关闭按钮时是否最小化而非退出
        /// </summary>
        /// <value>默认为true</value>
        public bool MinimizeOnClose { get; set; } = true;

        // 预设
        /// <summary>
        /// 获取或设置账号预设列表
        /// </summary>
        /// <value>默认包含"Default"</value>
        public List<string> AccountPresets { get; init; } = ["Default"];

        /// <summary>
        /// 获取或设置任务预设列表
        /// </summary>
        /// <value>默认包含"升级"和"维护"</value>
        public List<string> TaskPresets { get; set; } = ["升级", "维护"];

        /// <summary>
        /// 获取或设置账号列表
        /// </summary>
        public List<string> Accounts { get; set; } = [];

        /// <summary>
        /// 获取或设置界面字体规格（用于向后兼容）
        /// </summary>
        /// <remarks>
        /// 此属性不会被序列化到JSON中。
        /// 获取时会返回一个新的FontSpec实例，设置时会更新对应的字体属性。
        /// </remarks>
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