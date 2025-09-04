/*
 * 游戏升级提醒 - 任务管理窗口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 提供升级任务的管理界面，支持添加、编辑、删除和排序任务
 * 创建日期: 2025-08-15
 * 最后修改: 2025-09-02
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Models;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Services;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 简单的列表管理窗口，用于添加/删除字符串项。
    /// 通过构造函数的 <c>isAccountList</c> 参数区分“账号/任务”等不同列表类型。
    /// </summary>
    internal sealed class ManageListForm : Form
    {
        private readonly ListBox _lb = new() { IntegralHeight = false };
        private readonly Button _btnAdd = new();
        private readonly Button _btnEdit = new();
        private readonly Button _btnDel = new();
        private readonly Button _btnClose = new();

        /// <summary>
        /// 当前窗口中的列表项副本。编辑操作修改此集合；应用已采用“变更即保存”，
        /// 调用方通过 ItemsChanged 事件同步并持久化。
        /// </summary>
        private List<string> Items { get; }

        /// <summary>
        /// 列表变更事件：当用户新增/删除项后立即触发，供调用方即时保存。
        /// </summary>
        public event EventHandler<ItemsChangedEventArgs>? ItemsChanged;

        /// <summary>
        /// 单项重命名事件：当用户在本窗口对选中项执行“编辑”并确认新名称时触发。
        /// 订阅方可用于联动更新引用该名称的数据（例如任务列表中的账号/任务名）。
        /// </summary>
        public event EventHandler<ItemEditedEventArgs>? ItemEdited;

        /// <summary>
        /// 初始化管理窗口。
        /// 这会影响新增条目的提示与默认账号的保护逻辑。
        /// </summary>
        public ManageListForm(string title, bool isAccountList, List<string> items,
            ILocalizationService? localizationService = null)
        {
            var locService = localizationService ??
                             new JsonLocalizationService(Path.Combine(AppContext.BaseDirectory, "Resources",
                                 "Localization"));
            Text = title;
            Items = new List<string>(items);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 250);
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;

            // 根据语言自动应用 RTL，并在语言切换时动态更新
            RtlHelper.ApplyAndBind(locService, this);

            _btnAdd.Text = locService.GetText("Dialog.Add", "添加");
            _btnEdit.Text = locService.GetText("Dialog.Edit", "编辑");
            _btnDel.Text = locService.GetText("Dialog.Delete", "删除");
            _btnClose.Text = locService.GetText("Dialog.Complete", "完成");

            _lb.SetBounds(10, 10, 260, 210);
            _btnAdd.SetBounds(280, 10, 80, 26);
            _btnEdit.SetBounds(280, 46, 80, 26);
            _btnDel.SetBounds(280, 82, 80, 26);
            _btnClose.SetBounds(280, 194, 80, 26);

            foreach (var s in Items) _lb.Items.Add(s);

            _btnAdd.Click += (_, _) =>
            {
                var dialogTitle = isAccountList
                    ? locService.GetText("Dialog.AddAccount", "添加账号")
                    : locService.GetText("Dialog.AddTask", "添加任务");
                using var ib = new InputBox(locService, dialogTitle, locService.GetText("InputBox.Label.Name", "名称"));
                if (ib.ShowDialog(this) != DialogResult.OK) return;

                Items.Add(ib.ResultText);
                _lb.Items.Add(ib.ResultText);
                OnItemsChanged();
            };
            _btnDel.Click += (_, _) =>
            {
                var i = _lb.SelectedIndex;
                if (i < 0) return;

                Items.RemoveAt(i);
                _lb.Items.RemoveAt(i);

                // 若账号删除后为空则回填默认账号，避免主界面没有可选账号。
                if (Items.Count == 0 && isAccountList)
                {
                    Items.Add(TaskItem.DefaultAccount);
                    _lb.Items.Add(TaskItem.DefaultAccount);
                }

                OnItemsChanged();
            };
            _btnEdit.Click += (_, _) =>
            {
                var i = _lb.SelectedIndex;
                if (i < 0) return;

                var oldName = _lb.Items[i].ToString() ?? string.Empty;
                var dialogTitle = isAccountList
                    ? locService.GetText("Dialog.EditAccount", "编辑账号")
                    : locService.GetText("Dialog.EditTask", "编辑任务");
                using var ib = new InputBox(locService, dialogTitle, locService.GetText("InputBox.Label.Name", "名称"));

                // 预填旧值
                var tbField = typeof(InputBox).GetField("_tb",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (tbField?.GetValue(ib) is TextBox tb)
                {
                    tb.Text = oldName;
                    tb.SelectAll();
                }

                if (ib.ShowDialog(this) != DialogResult.OK) return;
                var newName = ib.ResultText;
                if (string.Equals(newName, oldName, StringComparison.Ordinal)) return;

                // 更新列表与数据
                Items[i] = newName;
                _lb.Items[i] = newName;
                OnItemsChanged();
                OnItemEdited(oldName, newName);
            };
            _btnClose.Click += (_, _) => { Close(); };

            Controls.AddRange([_lb, _btnAdd, _btnEdit, _btnDel, _btnClose]);
        }

        /// <summary>
        /// 触发 <see cref="ItemsChanged"/> 事件，向订阅方发送当前列表的快照。
        /// </summary>
        /// <remarks>
        /// - 始终发送列表的副本，避免订阅方修改内部状态。
        /// - 对订阅方抛出的异常进行捕获并忽略，以避免影响 UI 交互流程。
        /// </remarks>
        private void OnItemsChanged()
        {
            try
            {
                ItemsChanged?.Invoke(this, new ItemsChangedEventArgs([.. Items]));
            }
            catch
            {
                // 忽略
            }
        }

        /// <summary>
        /// 触发 <see cref="ItemEdited"/> 事件，携带旧/新名称。
        /// </summary>
        private void OnItemEdited(string oldName, string newName)
        {
            try
            {
                ItemEdited?.Invoke(this, new ItemEditedEventArgs(oldName, newName));
            }
            catch
            {
                // 忽略
            }
        }

        /// <summary>
        /// 事件参数：携带当前列表的快照。
        /// </summary>
        public sealed class ItemsChangedEventArgs(List<string> items) : EventArgs
        {
            public List<string> Items { get; } = items;
        }

        /// <summary>
        /// 单项编辑事件参数：包含旧名称与新名称。
        /// </summary>
        public sealed class ItemEditedEventArgs(string oldName, string newName) : EventArgs
        {
            public string OldName { get; } = oldName;
            public string NewName { get; } = newName;
        }
    }
}