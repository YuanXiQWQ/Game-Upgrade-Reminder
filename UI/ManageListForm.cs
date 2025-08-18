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
    /// <summary>
    /// 简单的列表管理窗口，用于添加/删除字符串项。
    /// 通过构造函数的 <c>title</c> 参数区分“账号/任务”等不同列表类型。
    /// </summary>
    internal sealed class ManageListForm : Form
    {
        private readonly ListBox lb = new() { IntegralHeight = false };
        private readonly Button btnAdd = new() { Text = "添加" };
        private readonly Button btnDel = new() { Text = "删除" };
        private readonly Button btnClose = new() { Text = "关闭" };

        /// <summary>
        /// 当前窗口中的列表项副本。编辑操作修改此集合，点击“关闭”后由调用方读取。
        /// </summary>
        public List<string> Items { get; private set; }

        /// <summary>
        /// 初始化管理窗口。
        /// title 用于指示列表类型（如以“账号”开头则视为账号列表），
        /// 这会影响新增条目的提示与默认账号的保护逻辑。
        /// </summary>
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

                // 若管理的是“账号”，且删除后为空，则回填默认账号，避免主界面没有可选账号。
                if (Items.Count > 0 || !title.StartsWith("账号")) return;

                Items.Add(TaskItem.DefaultAccount);
                lb.Items.Add(TaskItem.DefaultAccount);
            };
            btnClose.Click += (_, _) => { DialogResult = DialogResult.OK; };

            Controls.AddRange([lb, btnAdd, btnDel, btnClose]);
        }
    }
}
