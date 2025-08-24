/*
 * 游戏升级提醒 - 输入对话框
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供简单的文本输入对话框，用于用户输入账号名称等文本信息
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-23
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Services;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 简单的文本输入对话框（模态）。
    /// 调用者传入标题与标签文本，用户输入后点击“确定”即可通过 <see cref="ResultText"/> 获取结果。
    /// 典型用法：
    /// <code>
    /// using (var box = new InputBox(localizationService, "新增账号", label: "名称"))
    ///     if (box.ShowDialog(owner) == DialogResult.OK)
    ///     {
    ///         string name = box.ResultText;
    ///         // 使用 name 进行后续处理
    ///     }
    /// </code>
    /// </summary>
    internal sealed class InputBox : Form
    {
        /// <summary>
        /// 输入结果（去除首尾空白）。仅在点击“确定”且非空时生效。
        /// </summary>
        public string ResultText { get; private set; } = "";

        private readonly TextBox _tb = new();

        /// <summary>
        /// 创建输入框并设置标题与标签文字。
        /// </summary>
        /// <param name="localizationService">本地化服务</param>
        /// <param name="title">对话框标题</param>
        /// <param name="label">输入框左侧标签文字，默认为“名称”</param>
        public InputBox(ILocalizationService localizationService, string title, string label = "名称")
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 130);
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;

            // 根据语言自动应用 RTL，并在语言切换时动态更新
            RtlHelper.ApplyAndBind(localizationService, this);

            // 基本控件：标签、文本框、确定/取消按钮与布局参数
            var lb = new Label { Text = label, AutoSize = true, Left = 14, Top = 18 };
            _tb.SetBounds(76, 14, 250, 24);
            var ok = new Button { Text = localizationService.GetText("Common.OK", "确定"), Left = 116, Top = 54, Width = 80 };
            var cancel = new Button { Text = localizationService.GetText("Common.Cancel", "取消"), Left = 212, Top = 54, Width = 80 };

            // 点击“确定”：读取并裁剪文本；仅在非空时设置 DialogResult.OK
            ok.Click += (_, _) =>
            {
                ResultText = _tb.Text.Trim();
                if (ResultText.Length > 0) DialogResult = DialogResult.OK;
            };

            // 点击“取消”：直接关闭对话框，不修改 ResultText
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            Controls.Add(lb);
            Controls.Add(_tb);
            Controls.Add(ok);
            Controls.Add(cancel);
            // Enter=确定，Esc=取消（标准对话框行为）
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}