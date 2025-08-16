/*
 * 游戏升级提醒 - 任务管理窗口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供升级任务的管理界面，支持添加、编辑、删除和排序任务
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.UI
{
    internal sealed class ManageListForm : Form
    {
        private readonly ListBox lb = new() { IntegralHeight = false };
        private readonly Button btnAdd = new() { Text = "添加" };
        private readonly Button btnDel = new() { Text = "删除" };
        private readonly Button btnClose = new() { Text = "关闭" };

        public List<string> Items { get; private set; }

        public ManageListForm(string title, List<string> items)
        {
            Text = title;
            Items = new List<string>(items);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 250);
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;

            lb.SetBounds(10, 10, 260, 210);
            btnAdd.SetBounds(280, 10, 80, 26);
            btnDel.SetBounds(280, 46, 80, 26);
            btnClose.SetBounds(280, 194, 80, 26);

            foreach (var s in Items) lb.Items.Add(s);

            btnAdd.Click += (_, _) =>
            {
                using var ib = new InputBox(title.StartsWith("账号") ? "添加账号" : "添加任务");
                if (ib.ShowDialog(this) != DialogResult.OK) return;

                Items.Add(ib.ResultText);
                lb.Items.Add(ib.ResultText);
            };
            btnDel.Click += (_, _) =>
            {
                var i = lb.SelectedIndex;
                if (i < 0) return;

                Items.RemoveAt(i);
                lb.Items.RemoveAt(i);

                if (Items.Count > 0 || !title.StartsWith("账号")) return;

                Items.Add(TaskItem.DefaultAccount);
                lb.Items.Add(TaskItem.DefaultAccount);
            };
            btnClose.Click += (_, _) => { DialogResult = DialogResult.OK; };

            Controls.AddRange([lb, btnAdd, btnDel, btnClose]);
        }
    }
}
