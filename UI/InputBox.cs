/*
 * 游戏升级提醒 - 输入对话框
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供简单的文本输入对话框，用于用户输入账号名称等文本信息
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Drawing;
using System.Windows.Forms;

namespace Game_Upgrade_Reminder.UI
{
    internal sealed class InputBox : Form
    {
        public string ResultText { get; private set; } = "";
        private readonly TextBox tb = new();

        public InputBox(string title, string label = "名称")
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 130);
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;

            var lb = new Label { Text = label, AutoSize = true, Left = 14, Top = 18 };
            tb.SetBounds(76, 14, 250, 24);
            var ok = new Button { Text = "确定", Left = 116, Top = 54, Width = 80 };
            var cancel = new Button { Text = "取消", Left = 212, Top = 54, Width = 80 };
            ok.Click += (_, _) =>
            {
                ResultText = tb.Text.Trim();
                if (ResultText.Length > 0) DialogResult = DialogResult.OK;
            };
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            Controls.Add(lb);
            Controls.Add(tb);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}