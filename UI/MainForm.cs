/*
 * 游戏升级提醒 - 主窗体
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 游戏升级提醒主窗口，负责UI展示和用户交互，管理升级任务的显示和操作
 * 创建日期: 2025-08-15
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Game_Upgrade_Reminder.Core.Abstractions;
using Game_Upgrade_Reminder.Core.Services;
using Game_Upgrade_Reminder.Infrastructure.Repositories;
using Game_Upgrade_Reminder.Infrastructure.System;
using Game_Upgrade_Reminder.Infrastructure.UI;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder.UI
{
    public sealed class MainForm : Form
    {
        // 常量（保持与原实现一致）
        private const string AppTitle = "游戏升级提醒";
        private static readonly Color DueBackColor = Color.FromArgb(230, 230, 230);

        // 列宽
        private const int AccountColWidth = 150;
        private const int TaskColWidth = 150;
        private const int StartTimeColWidth = 130;
        private const int DurationColWidth = 150;
        private const int FinishTimeColWidth = 130;
        private const int RemainingTimeColWidth = 150;
        private const int ActionColWidth = 50;
        private const int ExtraSpace = 50;

        // 服务（最小拆分注入：直接在此构造即可）
        private readonly ITaskRepository taskRepo = new JsonTaskRepository();
        private readonly ISettingsStore settingsStore = new JsonSettingsStore();
        private readonly IAutostartManager autostartManager = new RegistryAutostartManager();
        private readonly ISortStrategy sortStrategy = new ByFinishTimeSortStrategy();
        private readonly IDeletionPolicy deletionPolicy = new SimpleDeletionPolicy(pendingDeleteDelaySeconds: 3, completedKeepMinutes: 1);
        private readonly IDurationFormatter durationFormatter = new ZhCnDurationFormatter();

        // 状态
        private SettingsData settings = new();
        private readonly BindingList<TaskItem> tasks = new();

        // ===== 新逻辑相关位（保持） =====
        private bool followSystemStartTime;
        private bool isUpdatingStartProgrammatically;
        private bool userEditingStart;

        private enum SortMode { DefaultByFinish, Custom }
        private SortMode sortMode = SortMode.DefaultByFinish;
        private int customSortColumn = 4;
        private bool customSortAsc = true;

        private readonly DateTimePicker dtpStart = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            ShowUpDown = true,
            Width = 160
        };

        // 字体（删除线）
        private Font? strikeFont;

        // 控件
        private readonly ComboBox cbAccount = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Button btnAccountMgr = new() { Text = "账号管理" };
        private readonly ComboBox cbTask = new() { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly Button btnTaskMgr = new() { Text = "任务管理" };
        private readonly Button btnDeleteDone = new() { Text = "删除已完成" };
        private readonly Button btnRefresh = new() { Text = "刷新" };
        private readonly Button btnNow = new() { Text = "当前时间" };
        private readonly NumericUpDown numDays = new() { Minimum = 0, Maximum = 3650, Width = 40 };
        private readonly NumericUpDown numHours = new() { Minimum = 0, Maximum = 1000, Width = 40 };
        private readonly NumericUpDown numMinutes = new() { Minimum = 0, Maximum = 59, Width = 40 };
        private readonly TextBox tbFinish = new() { ReadOnly = true };
        private readonly Button btnAddSave = new() { Text = "添加" };

        // 支持双缓冲的 ListView
        private class DoubleBufferedListView : ListView
        {
            public DoubleBufferedListView()
            {
                View = View.Details;
                FullRowSelect = true;
                HideSelection = false;
                MultiSelect = false;
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                if (DesignMode) return;
                DoubleBuffered = true;
                AllowDrop = true;
            }
        }

        private readonly ListView lv = new DoubleBufferedListView();

        // 菜单&托盘
        private readonly MenuStrip menu = new();
        private readonly ToolStripMenuItem miSettings = new() { Text = "设置(&S)" };
        private readonly ToolStripMenuItem miFont = new() { Text = "选择字体(&F)..." };
        private readonly ToolStripMenuItem miAutoStart = new() { Text = "开机自启(&A)" };
        private readonly ToolStripMenuItem miCloseExit = new() { Text = "退出程序" };
        private readonly ToolStripMenuItem miCloseMinimize = new() { Text = "最小化到托盘" };
        private readonly ToolStripMenuItem miHelp = new() { Text = "帮助(&H)" };
        private readonly ToolStripMenuItem miAbout = new() { Text = "关于(&A)..." };

        private readonly NotifyIcon tray = new();
        private readonly ContextMenuStrip trayMenu = new();
        private readonly INotifier notifier;

        // 计时器
        private readonly System.Windows.Forms.Timer timerTick = new() { Interval = 30_000 };
        private readonly System.Windows.Forms.Timer timerUi = new() { Interval = 1000 };
        private readonly System.Windows.Forms.Timer timerPurge = new() { Interval = 500 };

        // 拖拽排序
        private ListViewItem? dragItem;

        public MainForm()
        {
            Text = AppTitle;
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "YuanXi.ico");
            if (System.IO.File.Exists(iconPath))
            {
                try { Icon = new Icon(iconPath); } catch (Exception ex) { Console.WriteLine($"加载图标时出错: {ex.Message}"); }
            }

            const int totalWidth = AccountColWidth + TaskColWidth + StartTimeColWidth + DurationColWidth +
                                   FinishTimeColWidth + RemainingTimeColWidth + (ActionColWidth * 2) + ExtraSpace;
            ClientSize = new Size(totalWidth, 580);
            StartPosition = FormStartPosition.CenterScreen;

            notifier = new TrayNotifier(tray);

            BuildMenu();
            BuildUi();
            WireEvents();

            // 加载设置/任务
            LoadSettings();
            ApplySettingsToUi();
            LoadTasks();
            if (sortMode == SortMode.DefaultByFinish) sortStrategy.Sort(tasks);
            RefreshTable();

            // 自启状态对齐
            var actuallyOn = autostartManager.IsEnabled();
            if (actuallyOn != settings.StartupOnBoot)
            {
                settings.StartupOnBoot = actuallyOn;
                SaveSettings();
                UpdateMenuChecks();
            }

            // 计时器
            timerTick.Start();
            timerUi.Start();
            timerPurge.Start();

            // 初始：开始时间对齐系统时间并自动跟随
            isUpdatingStartProgrammatically = true;
            dtpStart.Value = DateTime.Now;
            isUpdatingStartProgrammatically = false;
            followSystemStartTime = true;
            RecalcFinishFromFields();

            // 托盘
            InitTray();
        }

        // ---------- 菜单/UI ----------
        private void BuildMenu()
        {
            var closeSub = new ToolStripMenuItem("关闭按钮行为(&C)");
            closeSub.DropDownItems.AddRange(new ToolStripItem[] { miCloseExit, miCloseMinimize });

            miSettings.DropDownItems.Add(miFont);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(miAutoStart);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(closeSub);

            miHelp.DropDownItems.Add(miAbout);

            menu.Items.Add(miSettings);
            menu.Items.Add(miHelp);
            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private void BuildUi()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(3);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            // ===== 行1：账号/任务/操作 =====
            var gbTop = new GroupBox { Text = "", Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 6) };
            var line1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RowCount = 1,
                ColumnCount = 11,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var lbAcc = MakeAutoLabel("账号");
            line1.Controls.Add(lbAcc, 0, 0);

            cbAccount.Width = 220;
            cbAccount.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbAccount.Margin = new Padding(0, 2, 6, 2);
            line1.Controls.Add(cbAccount, 1, 0);

            btnAccountMgr.Text = "管理账号";
            btnAccountMgr.AutoSize = true;
            btnAccountMgr.Anchor = AnchorStyles.Left;
            btnAccountMgr.Margin = new Padding(0, 2, 0, 2);
            line1.Controls.Add(btnAccountMgr, 2, 0);

            var lbTask = MakeAutoLabel("任务");
            line1.Controls.Add(lbTask, 4, 0);

            cbTask.Width = 240;
            cbTask.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbTask.Margin = new Padding(0, 2, 6, 2);
            line1.Controls.Add(cbTask, 5, 0);

            btnTaskMgr.Text = "管理任务";
            btnTaskMgr.AutoSize = true;
            btnTaskMgr.Anchor = AnchorStyles.Left;
            btnTaskMgr.Margin = new Padding(0, 2, 0, 2);
            line1.Controls.Add(btnTaskMgr, 6, 0);

            static void StyleSmallButton(Button b, Padding? margin = null)
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.Dock = DockStyle.None;
                b.Anchor = AnchorStyles.Left;
                b.Margin = margin ?? new Padding(6, 2, 0, 2);
                b.Padding = new Padding(6, 0, 6, 1);
                b.Height = 24;
                b.MinimumSize = new Size(0, 24);
            }

            btnDeleteDone.Text = "删除已完成";
            btnRefresh.Text = "刷新";
            StyleSmallButton(btnDeleteDone);
            StyleSmallButton(btnRefresh);

            line1.Controls.Add(btnDeleteDone, 8, 0);
            line1.Controls.Add(btnRefresh, 9, 0);

            gbTop.Controls.Add(line1);
            root.Controls.Add(gbTop, 0, 0);

            // ===== 行2：时间设置 =====
            var gbTime = new GroupBox { Text = "", Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 6) };
            var line2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RowCount = 1,
                ColumnCount = 14,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            line2.Controls.Add(MakeAutoLabel("开始时间"), 0, 0);

            dtpStart.Format = DateTimePickerFormat.Custom;
            dtpStart.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpStart.ShowUpDown = true;
            dtpStart.Margin = new Padding(0, 2, 6, 2);
            dtpStart.Anchor = AnchorStyles.Left;
            line2.Controls.Add(dtpStart, 1, 0);

            btnNow.Text = "当前时间";
            btnNow.AutoSize = true;
            btnNow.Anchor = AnchorStyles.Left;
            btnNow.Margin = new Padding(0, 0, 0, 0);
            line2.Controls.Add(btnNow, 2, 0);

            numDays.Width = 50; numDays.Margin = new Padding(0, 2, 4, 2); numDays.Anchor = AnchorStyles.Left;
            numHours.Width = 50; numHours.Margin = new Padding(0, 2, 4, 2); numHours.Anchor = AnchorStyles.Left;
            numMinutes.Width = 50; numMinutes.Margin = new Padding(0, 2, 4, 2); numMinutes.Anchor = AnchorStyles.Left;

            line2.Controls.Add(numDays, 4, 0);
            line2.Controls.Add(MakeAutoLabel("天"), 5, 0);
            line2.Controls.Add(numHours, 6, 0);
            line2.Controls.Add(MakeAutoLabel("小时"), 7, 0);
            line2.Controls.Add(numMinutes, 8, 0);
            line2.Controls.Add(MakeAutoLabel("分钟"), 9, 0);

            line2.Controls.Add(MakeAutoLabel("完成时间"), 10, 0);
            tbFinish.ReadOnly = true;
            tbFinish.Margin = new Padding(0, 2, 6, 2);
            tbFinish.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            line2.Controls.Add(tbFinish, 11, 0);

            btnAddSave.Text = "添加";
            StyleSmallButton(btnAddSave, new Padding(0, 2, 0, 2));
            line2.Controls.Add(btnAddSave, 12, 0);

            gbTime.Controls.Add(line2);
            root.Controls.Add(gbTime, 0, 1);

            // ===== 行3：任务列表 =====
            lv.Dock = DockStyle.Fill;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.View = View.Details;
            root.Controls.Add(lv, 0, 2);

            InitListViewColumns();
        }

        private void InitListViewColumns()
        {
            lv.Columns.Clear();
            lv.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "账号", Width = AccountColWidth },
                new ColumnHeader { Text = "任务", Width = TaskColWidth },
                new ColumnHeader { Text = "开始时间", Width = StartTimeColWidth },
                new ColumnHeader { Text = "持续时间", Width = DurationColWidth },
                new ColumnHeader { Text = "完成时间", Width = FinishTimeColWidth },
                new ColumnHeader { Text = "剩余时间", Width = RemainingTimeColWidth },
                new ColumnHeader { Text = "完成", Width = ActionColWidth },
                new ColumnHeader { Text = "删除", Width = ActionColWidth },
            });
        }

        private static Label MakeAutoLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 3, 6, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left
        };

        private void WireEvents()
        {
            // 菜单
            miFont.Click += (_, _) => DoChooseFont();
            miAutoStart.Click += (_, _) => ToggleAutostart();
            miCloseExit.Click += (_, _) =>
            {
                settings.MinimizeOnClose = false;
                SaveSettings();
                UpdateMenuChecks();
            };
            miCloseMinimize.Click += (_, _) =>
            {
                settings.MinimizeOnClose = true;
                SaveSettings();
                UpdateMenuChecks();
            };
            miAbout.Click += (_, _) => MessageBox.Show("海岛奇兵升级提醒", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 开始时间编辑识别
            dtpStart.MouseDown += (_, _) => userEditingStart = true;
            dtpStart.KeyDown += (_, _) => userEditingStart = true;
            dtpStart.CloseUp += (_, _) => userEditingStart = true;

            dtpStart.ValueChanged += (_, _) =>
            {
                if (!isUpdatingStartProgrammatically && userEditingStart) followSystemStartTime = false;
                userEditingStart = false;
                RecalcFinishFromFields();
            };
            numDays.ValueChanged += (_, _) => RecalcFinishFromFields();
            numHours.ValueChanged += (_, _) => RecalcFinishFromFields();
            numMinutes.ValueChanged += (_, _) => RecalcFinishFromFields();

            btnNow.Click += (_, _) =>
            {
                followSystemStartTime = true;
                isUpdatingStartProgrammatically = true;
                dtpStart.Value = DateTime.Now;
                isUpdatingStartProgrammatically = false;
                RecalcFinishFromFields();
            };

            btnAddSave.Click += (_, _) =>
            {
                followSystemStartTime = true;
                isUpdatingStartProgrammatically = true;
                dtpStart.Value = DateTime.Now;
                isUpdatingStartProgrammatically = false;

                RecalcFinishFromFields();
                AddOrSaveTask();
            };

            // 测试按钮（保持）
            var btnTest = new Button { Text = "测试排序", Location = new Point(10, 10), AutoSize = true };
            btnTest.Click += (_, _) => TestSorting();
            Controls.Add(btnTest);

            btnDeleteDone.Click += (_, _) => { DeleteAllDone(); SaveTasks(); RefreshTable(); };
            btnRefresh.Click += (_, _) => { PurgePending(force: true); RefreshTable(); };

            btnAccountMgr.Click += (_, _) => ShowManager(isAccount: true);
            btnTaskMgr.Click += (_, _) => ShowManager(isAccount: false);

            // ListView 行为
            lv.ItemActivate += (_, _) => HandleListClick();
            lv.MouseUp += (_, me) => { if (me.Button == MouseButtons.Left) HandleListClick(); };

            // 拖拽排序 -> 自定义模式
            lv.ItemDrag += (_, e) =>
            {
                if (e.Item == null) return;
                sortMode = SortMode.Custom;
                dragItem = (ListViewItem)e.Item;
                DoDragDrop(e.Item, DragDropEffects.Move);
            };
            lv.DragEnter += (_, e) => e.Effect = DragDropEffects.Move;
            lv.DragOver  += (_, e) => e.Effect = DragDropEffects.Move;
            lv.DragDrop  += Lv_DragDrop;

            // 列点击排序（与原一致：基于文本比较 + 特殊列解析）
            lv.ColumnClick += (_, e) =>
            {
                sortMode = SortMode.Custom;

                int column = e.Column;
                bool sortAscending = column != customSortColumn ? true : !customSortAsc;

                lv.ListViewItemSorter = new ListViewItemComparer(column, sortAscending);
                lv.Sort();

                customSortColumn = column;
                customSortAsc = sortAscending;

                var newOrder = new List<TaskItem>();
                foreach (ListViewItem row in lv.Items)
                    if (row.Tag is TaskItem tsk) newOrder.Add(tsk);

                tasks.Clear();
                foreach (var t in newOrder) tasks.Add(t);

                SaveTasks();
            };

            // Timer
            timerTick.Tick += (_, _) => CheckDueAndNotify();
            timerUi.Tick += (_, _) =>
            {
                UpdateRemainingCells();
                RepaintStyles();

                if (!followSystemStartTime || DateTime.Now.Second != 0) return;
                isUpdatingStartProgrammatically = true;
                dtpStart.Value = DateTime.Now;
                isUpdatingStartProgrammatically = false;
            };
            timerPurge.Tick += (_, _) => PurgePending(force: false);

            // 关闭最小化逻辑
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (settings.MinimizeOnClose)
            {
                e.Cancel = true;
                Hide();
                tray.BalloonTipTitle = "仍在运行";
                tray.BalloonTipText = "已最小化到托盘，双击图标可恢复。";
                tray.ShowBalloonTip(2000);
            }
            else
            {
                SaveTasks();
                SaveSettings();
                tray.Visible = false;
            }
        }

        private void Lv_DragDrop(object? sender, DragEventArgs e)
        {
            if (dragItem == null) return;
            var cp = lv.PointToClient(new Point(e.X, e.Y));
            var dest = lv.GetItemAt(cp.X, cp.Y) ?? (lv.Items.Count > 0 ? lv.Items[^1] : null);
            if (dest == null || dest.Index == dragItem.Index) return;

            var srcIdx = dragItem.Index;
            var dstIdx = dest.Index;

            var moving = tasks[srcIdx];
            if (srcIdx < dstIdx) { for (var i = srcIdx; i < dstIdx; i++) tasks[i] = tasks[i + 1]; }
            else { for (var i = srcIdx; i > dstIdx; i--) tasks[i] = tasks[i - 1]; }

            tasks[dstIdx] = moving;

            SaveTasks();
            RefreshTable();
            dragItem = null;
        }

        // 与原实现一致的比较器
        private class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int col;
            private readonly bool asc;

            public ListViewItemComparer(int column, bool ascending = true)
            {
                col = column;
                asc = ascending;
            }

            public int Compare(object? x, object? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                if (x is not ListViewItem lvx || y is not ListViewItem lvy) return 0;

                var xText = col < lvx.SubItems.Count ? lvx.SubItems[col].Text : string.Empty;
                var yText = col < lvy.SubItems.Count ? lvy.SubItems[col].Text : string.Empty;

                // 时间列
                if (col == 2 || col == 4)
                {
                    if (DateTime.TryParse(xText, out var xDate) && DateTime.TryParse(yText, out var yDate))
                        return asc ? xDate.CompareTo(yDate) : yDate.CompareTo(xDate);
                }
                // 时长列
                else if (col == 3 || col == 5)
                {
                    var xSecs = ParseTimeSpanToSeconds(xText);
                    var ySecs = ParseTimeSpanToSeconds(yText);
                    return asc ? xSecs.CompareTo(ySecs) : ySecs.CompareTo(xSecs);
                }

                var result = string.Compare(xText, yText, StringComparison.Ordinal);

                // 二级：按剩余时间升序
                if (result == 0 && col != 5)
                {
                    var xRemaining = ParseTimeSpanToSeconds(lvx.SubItems[5].Text);
                    var yRemaining = ParseTimeSpanToSeconds(lvy.SubItems[5].Text);
                    return xRemaining.CompareTo(yRemaining);
                }

                return asc ? result : -result;
            }

            private static int ParseTimeSpanToSeconds(string text)
            {
                if (string.IsNullOrEmpty(text)) return 0;
                int totalSeconds = 0;
                var numberStr = new StringBuilder();

                for (int i = 0; i < text.Length; i++)
                {
                    if (char.IsDigit(text[i])) numberStr.Append(text[i]);
                    else if (numberStr.Length > 0)
                    {
                        int value = int.Parse(numberStr.ToString());
                        numberStr.Clear();

                        if (text[i] == '天') totalSeconds += value * 24 * 3600;
                        else if (i < text.Length - 1 && text[i] == '小' && text[i + 1] == '时') { totalSeconds += value * 3600; i++; }
                        else if (i < text.Length - 1 && text[i] == '分' && text[i + 1] == '钟') { totalSeconds += value * 60; i++; }
                        else if (text[i] == '时') totalSeconds += value * 3600;
                        else if (text[i] == '分') totalSeconds += value * 60;
                        else if (text[i] == '秒') totalSeconds += value;
                    }
                }

                if (numberStr.Length > 0) totalSeconds += int.Parse(numberStr.ToString());
                return totalSeconds;
            }
        }

        private void TestSorting()
        {
            try
            {
                var originalTasks = new List<TaskItem>(tasks);
                tasks.Clear();
                var now = DateTime.Now;

                tasks.Add(new TaskItem
                {
                    Account = "UserB",
                    TaskName = "Upgrade Defense",
                    Start = now,
                    Days = 1,
                    Hours = 2,
                    Minutes = 30,
                    Finish = now.AddDays(1).AddHours(2).AddMinutes(30)
                });

                tasks.Add(new TaskItem
                {
                    Account = "UserA",
                    TaskName = "Upgrade Resource",
                    Start = now.AddHours(-2),
                    Days = 0,
                    Hours = 12,
                    Minutes = 0,
                    Finish = now.AddHours(10)
                });

                tasks.Add(new TaskItem
                {
                    Account = "UserA",
                    TaskName = "Research",
                    Start = now.AddHours(-1),
                    Days = 2,
                    Hours = 0,
                    Minutes = 15,
                    Finish = now.AddDays(2).AddMinutes(15)
                });

                RefreshTable();

                lv.ListViewItemSorter = new ListViewItemComparer(0); lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(1); lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(2); lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(3); lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(4); lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(5); lv.Sort();

                tasks.Clear();
                foreach (var item in originalTasks) tasks.Add(item);

                RefreshTable();

                MessageBox.Show("排序测试完成！", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"排序测试失败：{ex.Message}", "测试错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------- 设置 & 持久化 ----------
        private void LoadSettings()
        {
            settings = settingsStore.Load();

            // 如果首次无文件：使用注册表状态初始化 StartupOnBoot，并写入模板
            if (!System.IO.File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "settings.json")))
            {
                settings.StartupOnBoot = autostartManager.IsEnabled();
                SaveSettings();
            }

            UpdateMenuChecks();
        }

        private void SaveSettings() => settingsStore.Save(settings);

        private void LoadTasks()
        {
            tasks.Clear();
            foreach (var t in taskRepo.Load()) tasks.Add(t);
        }

        private void SaveTasks() => taskRepo.Save(tasks);

        // ---------- UI 应用设置 ----------
        private void ApplySettingsToUi()
        {
            cbAccount.Items.Clear();
            foreach (var a in settings.Accounts) cbAccount.Items.Add(a);
            if (cbAccount.Items.Count == 0) cbAccount.Items.Add(TaskItem.DefaultAccount);
            cbAccount.SelectedIndex = 0;

            cbTask.Items.Clear();
            foreach (var t in settings.TaskPresets) cbTask.Items.Add(t);

            try
            {
                var f = settings.UiFont.ToFont();
                ApplyUiFont(f);
            }
            catch { /* ignore */ }
        }

        private void UpdateMenuChecks()
        {
            miAutoStart.Checked = settings.StartupOnBoot;
            miCloseExit.Checked = !settings.MinimizeOnClose;
            miCloseMinimize.Checked = settings.MinimizeOnClose;
        }

        private void ApplyUiFont(Font f)
        {
            Font = f;
            ApplyFontRecursive(this, f);
            strikeFont?.Dispose();
            strikeFont = new Font(f, f.Style | FontStyle.Strikeout);
        }

        private static void ApplyFontRecursive(Control parent, Font f)
        {
            foreach (Control c in parent.Controls)
            {
                c.Font = f;
                if (c.HasChildren) ApplyFontRecursive(c, f);
            }
        }

        private void DoChooseFont()
        {
            using var dlg = new FontDialog();
            try { dlg.ShowEffects = true; dlg.Font = Font; } catch { /* ignore */ }
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            ApplyUiFont(dlg.Font);
            settings.UiFont = FontSpec.From(dlg.Font);
            SaveSettings();
        }

        // ---------- 托盘 ----------
        private void InitTray()
        {
            tray.Icon = Icon;
            tray.Text = "升级提醒";
            tray.Visible = true;
            tray.DoubleClick += (_, _) => { Show(); Activate(); };

            trayMenu.Items.Add("打开(&O)", null, (_, _) => { Show(); Activate(); });
            trayMenu.Items.Add("退出(&X)", null, (_, _) =>
            {
                settings.MinimizeOnClose = false;
                Close();
            });
            tray.ContextMenuStrip = trayMenu;
        }

        // ---------- 列表 ----------
        private void RefreshTable()
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            foreach (var t in tasks)
            {
                var duration = durationFormatter.Format(t.Days, t.Hours, t.Minutes);
                var it = new ListViewItem(new[]
                {
                    t.Account,
                    t.TaskName,
                    t.StartStr,
                    duration,
                    t.FinishStr,
                    t.RemainingStr,
                    t.Done ? "撤销完成" : "完成",
                    t.PendingDelete ? "撤销删除" : "删除"
                });
                it.Tag = t;
                lv.Items.Add(it);
            }

            RepaintStyles();
            lv.EndUpdate();

            if (sortMode == SortMode.Custom)
            {
                lv.ListViewItemSorter = new ListViewItemComparer(customSortColumn, customSortAsc);
                lv.Sort();
            }
        }

        private void UpdateRemainingCells()
        {
            foreach (ListViewItem row in lv.Items)
            {
                if (row.Tag is TaskItem t) row.SubItems[5].Text = t.RemainingStr;
            }
        }

        private void RepaintStyles()
        {
            foreach (ListViewItem row in lv.Items)
            {
                if (row.Tag is not TaskItem t) continue;

                row.BackColor = Color.White;
                row.ForeColor = Color.Black;
                row.Font = Font;

                if (t.PendingDelete && strikeFont != null)
                {
                    row.Font = strikeFont;
                }
                else if (t.Done)
                {
                    row.ForeColor = Color.Gray;
                    row.BackColor = Color.White;
                }
                else if (t.Finish <= DateTime.Now)
                {
                    row.BackColor = DueBackColor;
                }

                row.SubItems[6].Text = t.Done ? "撤销完成" : "完成";
                row.SubItems[7].Text = t.PendingDelete ? "撤销删除" : "删除";
            }
        }

        private int SelectedIndex => lv.SelectedIndices.Count > 0 ? lv.SelectedIndices[0] : -1;

        private void HandleListClick()
        {
            var idx = SelectedIndex;
            if (idx < 0 || idx >= tasks.Count) return;

            var hit = lv.PointToClient(MousePosition);
            var info = lv.HitTest(hit);
            if (info.Item == null) return;
            var sub = info.Item.SubItems.IndexOf(info.SubItem);

            var t = tasks[idx];

            switch (sub)
            {
                case 6:
                    t.Done = !t.Done;
                    t.CompletedTime = t.Done ? DateTime.Now : null;
                    SaveTasks();
                    RefreshTable();
                    break;

                case 7:
                    t.PendingDelete = !t.PendingDelete;
                    t.DeleteMarkTime = t.PendingDelete ? DateTime.Now : null;
                    SaveTasks();
                    RefreshTable();
                    break;

                case >= 0 and < 6:
                    cbAccount.Text = t.Account;
                    cbTask.Text = t.TaskName;

                    isUpdatingStartProgrammatically = true;
                    dtpStart.Value = t.Start ?? DateTime.Now;
                    isUpdatingStartProgrammatically = false;

                    numDays.Value = t.Days;
                    numHours.Value = t.Hours;
                    numMinutes.Value = t.Minutes;

                    RecalcFinishFromFields();
                    break;
            }
        }

        // ---------- 实时计算 ----------
        private void RecalcFinishFromFields()
        {
            var st = dtpStart.Value;
            var d = (int)numDays.Value;
            var h = (int)numHours.Value;
            var m = (int)numMinutes.Value;

            var fin = st.AddDays(d).AddHours(h).AddMinutes(m);
            tbFinish.Text = fin.ToString(TaskItem.TimeFormat);
        }

        private string GetAccountText()
        {
            if (cbAccount.SelectedItem is string s && !string.IsNullOrWhiteSpace(s)) return s;
            return TaskItem.DefaultAccount;
        }

        private void AddOrSaveTask()
        {
            var acc = GetAccountText();
            var taskName = string.IsNullOrWhiteSpace(cbTask.Text) ? "-" : cbTask.Text.Trim();
            var st = dtpStart.Value;

            var d = (int)numDays.Value;
            var h = (int)numHours.Value;
            var m = (int)numMinutes.Value;

            var fin = st.AddDays(d).AddHours(h).AddMinutes(m);

            var t = new TaskItem
            {
                Account = acc,
                TaskName = taskName,
                Start = st,
                Days = d,
                Hours = h,
                Minutes = m,
                Finish = fin,
                Notified = false,
                Done = false,
                PendingDelete = false,
                DeleteMarkTime = null
            };

            var sel = SelectedIndex;
            if (sel >= 0 && sel < tasks.Count)
            {
                tasks[sel] = t;
                if (sortMode == SortMode.DefaultByFinish)
                {
                    var moved = tasks[sel];
                    tasks.RemoveAt(sel);
                    sortStrategy.Insert(tasks, moved);
                }
            }
            else
            {
                if (sortMode == SortMode.DefaultByFinish) sortStrategy.Insert(tasks, t);
                else tasks.Add(t);
            }

            SaveTasks();
            RefreshTable();
        }

        private void DeleteAllDone()
        {
            for (var i = 0; i < tasks.Count;)
            {
                if (tasks[i].Done) tasks.RemoveAt(i);
                else i++;
            }
        }

        // ---------- 到点通知 & 延迟删除 ----------
        private void CheckDueAndNotify()
        {
            var changed = false;
            foreach (var t in tasks)
            {
                if (t.Finish > DateTime.Now || t.Notified) continue;

                notifier.Toast($"[到点] {t.Account}", $"{t.TaskName} 完成时间：{t.FinishStr}");
                t.Notified = true;
                changed = true;
            }

            if (changed) SaveTasks();
        }

        private void PurgePending(bool force)
        {
            var changed = false;
            var now = DateTime.Now;

            for (var i = 0; i < tasks.Count;)
            {
                var t = tasks[i];
                if (deletionPolicy.ShouldRemove(t, now, force))
                {
                    tasks.RemoveAt(i);
                    changed = true;
                }
                else i++;
            }

            if (!changed) return;

            if (sortMode == SortMode.DefaultByFinish) sortStrategy.Sort(tasks);
            SaveTasks();
            RefreshTable();
        }

        // ---------- 管理窗口 ----------
        private void ShowManager(bool isAccount)
        {
            using var dlg = new ManageListForm(isAccount ? "账号管理" : "任务管理",
                isAccount ? settings.Accounts : settings.TaskPresets);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (isAccount)
                {
                    settings.Accounts = dlg.Items;
                    cbAccount.Items.Clear();
                    foreach (var a in settings.Accounts) cbAccount.Items.Add(a);
                    if (cbAccount.Items.Count == 0) cbAccount.Items.Add(TaskItem.DefaultAccount);
                    cbAccount.SelectedIndex = 0;
                }
                else
                {
                    settings.TaskPresets = dlg.Items;
                    cbTask.Items.Clear();
                    foreach (var t in settings.TaskPresets) cbTask.Items.Add(t);
                }

                SaveSettings();
            }
        }

        // ---------- 开机自启 ----------
        private void ToggleAutostart()
        {
            settings.StartupOnBoot = !settings.StartupOnBoot;
            try
            {
                autostartManager.SetEnabled(settings.StartupOnBoot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机自启失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SaveSettings();
                UpdateMenuChecks();
            }
        }
    }
}
