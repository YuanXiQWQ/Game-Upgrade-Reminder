/*
 * 游戏升级提醒 - 主程序入口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 游戏升级提醒工具，用于跟踪和管理游戏中的升级进度
 * 创建日期: 2025-08-13
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Game_Upgrade_Reminder.Models;

namespace Game_Upgrade_Reminder
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Handle command line arguments
            var startMinimized = args is { Length: > 0 } &&
                                 string.Equals(args[0], "--minimized", StringComparison.OrdinalIgnoreCase);

            var mainForm = new MainForm();
            if (startMinimized)
            {
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.Hide();
            }

            Application.Run(mainForm);
        }
    }

    // ---------- 主窗体 ----------
    public sealed class MainForm : Form
    {
        // 常量
        private const string AppClass = "Game_Upgrade_Reminder";
        private const string AppTitle = "游戏升级提醒";
        private static readonly Color DueBackColor = Color.FromArgb(230, 230, 230);

        // 列宽常量
        private const int AccountColWidth = 150;
        private const int TaskColWidth = 150;
        private const int StartTimeColWidth = 130;
        private const int DurationColWidth = 150;
        private const int FinishTimeColWidth = 130;
        private const int RemainingTimeColWidth = 150;
        private const int ActionColWidth = 50;
        private const int ExtraSpace = 50;

        // 文件路径
        private static string AppBaseDir => AppContext.BaseDirectory;
        private static string SettingsPath => Path.Combine(AppBaseDir, "settings.json");
        private static string TasksPath => Path.Combine(AppBaseDir, "tasks.json");

        // 状态
        private SettingsData settings = new();
        private readonly BindingList<TaskItem> tasks = new();

        // ===== 新逻辑相关位 =====
        private bool followSystemStartTime;

        // 程序内部正在设置 dtpStart.Value（防止误判为手动编辑）
        private bool isUpdatingStartProgrammatically;

        // 用户正在通过鼠标/键盘操作编辑开始时间
        private bool userEditingStart;

        // 排序模式：默认按完成时间，或用户自定义（列点击/拖拽）
        private enum SortMode
        {
            DefaultByFinish,
            Custom
        }

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
                if (DesignMode)
                    return;

                // 启用双缓冲
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

        // 计时器
        private readonly System.Windows.Forms.Timer timerTick = new() { Interval = 30_000 }; // 到点检查
        private readonly System.Windows.Forms.Timer timerUi = new() { Interval = 1000 }; // 每秒刷新剩余时间
        private readonly System.Windows.Forms.Timer timerPurge = new() { Interval = 500 }; // 延迟删除检查

        private const int PendingDeleteDelaySeconds = 3;

        // 拖拽排序
        private ListViewItem? dragItem;

        public MainForm()
        {
            Text = AppTitle;
            var iconPath = Path.Combine(AppBaseDir, "YuanXi.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    Icon = new Icon(iconPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载图标时出错: {ex.Message}");
                }
            }

            const int totalWidth = AccountColWidth + TaskColWidth + StartTimeColWidth + DurationColWidth +
                                   FinishTimeColWidth + RemainingTimeColWidth + (ActionColWidth * 2) + ExtraSpace;
            ClientSize = new Size(totalWidth, 580);
            StartPosition = FormStartPosition.CenterScreen;

            BuildMenu();
            BuildUi();
            WireEvents();

            LoadSettings();
            ApplySettingsToUi();
            LoadTasks();
            if (sortMode == SortMode.DefaultByFinish) SortByFinish();
            RefreshTable();

            // 开机自启状态与注册表对齐
            var actuallyOn = QueryAutostart();
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

            // 初始：开始时间对齐当前系统时间，并处于自动跟随
            isUpdatingStartProgrammatically = true;
            dtpStart.Value = DateTime.Now;
            isUpdatingStartProgrammatically = false;
            followSystemStartTime = true;
            RecalcFinishFromFields();

            // 托盘
            InitTray();
        }

        // ---------- 构建菜单 ----------
        private void BuildMenu()
        {
            // 设置
            var closeSub = new ToolStripMenuItem("关闭按钮行为(&C)");
            closeSub.DropDownItems.AddRange(new ToolStripItem[] { miCloseExit, miCloseMinimize });

            miSettings.DropDownItems.Add(miFont);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(miAutoStart);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(closeSub);

            // 帮助
            miHelp.DropDownItems.Add(miAbout);

            menu.Items.Add(miSettings);
            menu.Items.Add(miHelp);
            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        // ---------- 构建 UI ----------
        private void BuildUi()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(3);

            // 根表格
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 行1：账号/任务/操作
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 行2：时间设置
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // 行3：列表
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

            // 列： [账号lbl][账号下拉][管理账号][gap][任务lbl][任务下拉][管理任务][gap][排序][删除完成][刷新][stretch]
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 0  账号lbl
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // 1  账号下拉
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 2  管理账号
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14)); // 3  gap
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 4  任务lbl
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240)); // 5  任务下拉
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 6  管理任务
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6)); // 7  gap
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 9  删除完成
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 10 刷新
            line1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // 11 伸展填充

            // 账号
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

            // 任务
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

            // [开始时间lbl][开始dtp][当前时间][gap][天num][天lbl][小时num][小时lbl][分钟num][分钟lbl][完成lbl][完成tb][添加][stretch]
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 0  开始时间lbl
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // 1  dtp
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 2  当前时间
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14)); // 3  gap
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // 4  天num
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 5  天lbl
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // 6  小时num
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 7  小时lbl
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // 8  分钟num
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 9  分钟lbl
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // 10 完成lbl
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // 11 完成tb
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // 12 添加
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // 13 拉伸余量

            // 开始时间
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

            // 天/小时/分钟
            numDays.Width = 50;
            numDays.Margin = new Padding(0, 2, 4, 2);
            numDays.Anchor = AnchorStyles.Left;
            numHours.Width = 50;
            numHours.Margin = new Padding(0, 2, 4, 2);
            numHours.Anchor = AnchorStyles.Left;
            numMinutes.Width = 50;
            numMinutes.Margin = new Padding(0, 2, 4, 2);
            numMinutes.Anchor = AnchorStyles.Left;

            line2.Controls.Add(numDays, 4, 0);
            line2.Controls.Add(MakeAutoLabel("天"), 5, 0);
            line2.Controls.Add(numHours, 6, 0);
            line2.Controls.Add(MakeAutoLabel("小时"), 7, 0);
            line2.Controls.Add(numMinutes, 8, 0);
            line2.Controls.Add(MakeAutoLabel("分钟"), 9, 0);

            // 完成时间
            line2.Controls.Add(MakeAutoLabel("完成时间"), 10, 0);
            tbFinish.ReadOnly = true;
            tbFinish.Margin = new Padding(0, 2, 6, 2);
            tbFinish.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            line2.Controls.Add(tbFinish, 11, 0);

            // 添加
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

        private static Label MakeAutoLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 3, 6, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };
        }

        // ---------- 事件 ----------
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
            miAbout.Click += (_, _) => MessageBox.Show("海岛奇兵升级提醒", "关于", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // ===== 开始时间编辑相关：用事件识别“用户正在编辑” =====
            dtpStart.MouseDown += (_, _) => userEditingStart = true;
            dtpStart.KeyDown += (_, _) => userEditingStart = true;
            dtpStart.CloseUp += (_, _) => userEditingStart = true;

            // 字段变化 -> 计算完成时间
            dtpStart.ValueChanged += (_, _) =>
            {
                // 只有当不是程序内部更新，且确实来自用户的编辑，才停止自动跟随
                if (!isUpdatingStartProgrammatically && userEditingStart)
                {
                    followSystemStartTime = false;
                }

                userEditingStart = false; // 结束本次用户编辑周期
                RecalcFinishFromFields();
            };
            numDays.ValueChanged += (_, _) => RecalcFinishFromFields();
            numHours.ValueChanged += (_, _) => RecalcFinishFromFields();
            numMinutes.ValueChanged += (_, _) => RecalcFinishFromFields();

            btnNow.Click += (_, _) =>
            {
                // 恢复自动跟随，并同步到当前系统时间
                followSystemStartTime = true;
                isUpdatingStartProgrammatically = true;
                dtpStart.Value = DateTime.Now;
                isUpdatingStartProgrammatically = false;

                RecalcFinishFromFields();
            };

            btnAddSave.Click += (_, _) =>
            {
                // 保存前：开始时间回到当前系统时间，并恢复自动跟随
                followSystemStartTime = true;
                isUpdatingStartProgrammatically = true;
                dtpStart.Value = DateTime.Now;
                isUpdatingStartProgrammatically = false;

                RecalcFinishFromFields();
                AddOrSaveTask();
            };

            // 添加测试按钮
            var btnTest = new Button
            {
                Text = "测试排序",
                Location = new Point(10, 10),
                AutoSize = true
            };
            btnTest.Click += (_, _) => TestSorting();
            Controls.Add(btnTest);

            btnDeleteDone.Click += (_, _) =>
            {
                DeleteAllDone();
                SaveTasks();
                RefreshTable();
            };
            btnRefresh.Click += (_, _) =>
            {
                PurgePending(force: true);
                RefreshTable();
            };

            btnAccountMgr.Click += (_, _) => ShowManager(isAccount: true);
            btnTaskMgr.Click += (_, _) => ShowManager(isAccount: false);

            // ListView 行为
            lv.ItemActivate += (_, _) => HandleListClick();
            lv.MouseUp += (_, me) =>
            {
                if (me.Button == MouseButtons.Left) HandleListClick();
            };

            // 拖拽排序（视为显式排序，切换到自定义模式）
            lv.ItemDrag += (_, e) =>
            {
                if (e.Item == null) return;
                sortMode = SortMode.Custom;
                dragItem = (ListViewItem)e.Item;
                DoDragDrop(e.Item, DragDropEffects.Move);
            };
            lv.DragEnter += (_, e) => e.Effect = DragDropEffects.Move;
            lv.DragOver += (_, e) => e.Effect = DragDropEffects.Move;
            lv.DragDrop += Lv_DragDrop;

            // 列标题点击排序（显式）
            lv.ColumnClick += (_, e) =>
            {
                // 用户显式点列：进入自定义排序模式
                sortMode = SortMode.Custom;

                int column = e.Column;
                bool sortAscending = column != customSortColumn ? true : !customSortAsc;

                lv.ListViewItemSorter = new ListViewItemComparer(column, sortAscending);
                lv.Sort();

                // 保存当前状态
                customSortColumn = column;
                customSortAsc = sortAscending;

                // 按可见顺序回写 tasks（通过 Tag 绑定）
                var newOrder = new List<TaskItem>();
                foreach (ListViewItem row in lv.Items)
                {
                    if (row.Tag is TaskItem tsk) newOrder.Add(tsk);
                }

                tasks.Clear();
                foreach (var tsk in newOrder) tasks.Add(tsk);

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
            var dest = lv.GetItemAt(cp.X, cp.Y);
            dest ??= lv.Items.Count > 0 ? lv.Items[^1] : null;
            if (dest == null || dest.Index == dragItem.Index) return;

            var srcIdx = dragItem.Index;
            var dstIdx = dest.Index;

            var moving = tasks[srcIdx];
            if (srcIdx < dstIdx)
            {
                for (var i = srcIdx; i < dstIdx; i++) tasks[i] = tasks[i + 1];
            }
            else
            {
                for (var i = srcIdx; i > dstIdx; i--) tasks[i] = tasks[i - 1];
            }

            tasks[dstIdx] = moving;

            SaveTasks();
            RefreshTable();
            dragItem = null;
        }

        // 列表项比较器，用于排序
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

                if (!(x is ListViewItem lvx) || !(y is ListViewItem lvy)) return 0;

                var xText = col < lvx.SubItems.Count ? lvx.SubItems[col].Text : string.Empty;
                var yText = col < lvy.SubItems.Count ? lvy.SubItems[col].Text : string.Empty;

                // 如果是时间列（开始时间、完成时间）
                if (col == 2 || col == 4) // 开始时间或完成时间
                {
                    if (DateTime.TryParse(xText, out var xDate) && DateTime.TryParse(yText, out var yDate))
                    {
                        return asc ? xDate.CompareTo(yDate) : yDate.CompareTo(xDate);
                    }
                }
                // 如果是时长列（持续时间、剩余时间）
                else if (col == 3 || col == 5) // 持续时间或剩余时间
                {
                    var xSecs = ParseTimeSpanToSeconds(xText);
                    var ySecs = ParseTimeSpanToSeconds(yText);
                    return asc ? xSecs.CompareTo(ySecs) : ySecs.CompareTo(xSecs);
                }

                // 默认文本比较
                var result = string.Compare(xText, yText, StringComparison.Ordinal);

                // 如果主排序列值相同，则按剩余时间升序排序
                if (result == 0 && col != 5) // 5是剩余时间列，避免重复排序
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
                var numberStr = new System.Text.StringBuilder();

                for (int i = 0; i < text.Length; i++)
                {
                    if (char.IsDigit(text[i]))
                    {
                        numberStr.Append(text[i]);
                    }
                    else if (numberStr.Length > 0)
                    {
                        int value = int.Parse(numberStr.ToString());
                        numberStr.Clear();

                        if (text[i] == '天')
                        {
                            totalSeconds += value * 24 * 3600;
                        }
                        else if (i < text.Length - 1 && text[i] == '小' && text[i + 1] == '时')
                        {
                            totalSeconds += value * 3600;
                            i++; // 跳过'时'
                        }
                        else if (i < text.Length - 1 && text[i] == '分' && text[i + 1] == '钟')
                        {
                            totalSeconds += value * 60;
                            i++; // 跳过'钟'
                        }
                        else if (text[i] == '时')
                        {
                            totalSeconds += value * 3600;
                        }
                        else if (text[i] == '分')
                        {
                            totalSeconds += value * 60;
                        }
                        else if (text[i] == '秒')
                        {
                            totalSeconds += value;
                        }
                    }
                }

                // 处理最后一个数字（如果没有单位，默认是“秒”）
                if (numberStr.Length > 0)
                {
                    totalSeconds += int.Parse(numberStr.ToString());
                }

                return totalSeconds;
            }
        }

        private void TestSorting()
        {
            try
            {
                // 保存当前任务列表
                var originalTasks = new List<TaskItem>(tasks);

                // 添加测试数据
                tasks.Clear();
                var now = DateTime.Now;

                // 添加测试任务
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

                // 刷新列表
                RefreshTable();

                // 测试各种列排序
                lv.ListViewItemSorter = new ListViewItemComparer(0);
                lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(1);
                lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(2);
                lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(3);
                lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(4);
                lv.Sort();
                lv.ListViewItemSorter = new ListViewItemComparer(5);
                lv.Sort();

                // 恢复原始任务列表
                tasks.Clear();
                foreach (var item in originalTasks)
                {
                    tasks.Add(item);
                }

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
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    // 根据注册表初始化自启默认值
                    settings.StartupOnBoot = QueryAutostart();
                    SaveSettings(); // 写默认模板
                }
                else
                {
                    var json = File.ReadAllText(SettingsPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    settings = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
            }
            catch
            {
                settings = new SettingsData();
            }

            UpdateMenuChecks();
        }

        private void SaveSettings()
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, opt), utf8Bom);
            }
            catch
            {
                // ignore
            }
        }

        private void LoadTasks()
        {
            tasks.Clear();
            try
            {
                if (!File.Exists(TasksPath)) return;
                var json = File.ReadAllText(TasksPath, new UTF8Encoding(false));
                var list = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
                foreach (var t in list) tasks.Add(t);
            }
            catch
            {
                // ignore
            }
        }

        private void SaveTasks()
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(TasksPath, JsonSerializer.Serialize(tasks, opt), utf8Bom);
            }
            catch
            {
                // ignore
            }
        }

        // ---------- UI 应用设置 ----------
        private void ApplySettingsToUi()
        {
            // 账号下拉
            cbAccount.Items.Clear();
            foreach (var a in settings.Accounts) cbAccount.Items.Add(a);
            if (cbAccount.Items.Count == 0) cbAccount.Items.Add(TaskItem.DefaultAccount);
            cbAccount.SelectedIndex = 0;

            // 任务预设
            cbTask.Items.Clear();
            foreach (var t in settings.TaskPresets) cbTask.Items.Add(t);

            // 字体
            try
            {
                var f = settings.UiFont.ToFont();
                ApplyUiFont(f);
            }
            catch
            {
                // ignore
            }
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
            try
            {
                dlg.ShowEffects = true;
                dlg.Font = Font;
            }
            catch
            {
                // ignore
            }

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
            tray.DoubleClick += (_, _) =>
            {
                Show();
                Activate();
            };

            trayMenu.Items.Add("打开(&O)", null, (_, _) =>
            {
                Show();
                Activate();
            });
            trayMenu.Items.Add("退出(&X)", null, (_, _) =>
            {
                settings.MinimizeOnClose = false;
                Close();
            });
            tray.ContextMenuStrip = trayMenu;
        }

        private void Toast(string title, string body)
        {
            tray.BalloonTipTitle = title;
            tray.BalloonTipText = body;
            tray.ShowBalloonTip(3000);
        }

        // ---------- 任务表 ----------
        private void RefreshTable()
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            foreach (var t in tasks)
            {
                var duration = FormatTime(t.Days, t.Hours, t.Minutes);
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

        private static string FormatTime(int days, int hours, int minutes, int seconds = 0, bool showSeconds = false)
        {
            var parts = new List<string>();

            if (days > 0)
            {
                parts.Add($"{days}天");
                if (hours > 0 || (hours == 0 && (minutes > 0 || (showSeconds && seconds > 0))))
                {
                    parts.Add($"{hours}小时");
                    if (minutes > 0 || (showSeconds && seconds > 0))
                    {
                        parts.Add($"{minutes}分钟");
                        if (showSeconds && seconds > 0)
                        {
                            parts.Add($"{seconds}秒");
                        }
                    }
                }
                else if (minutes > 0)
                {
                    parts.Add($"0小时 {minutes}分钟");
                    if (showSeconds && seconds > 0)
                    {
                        parts.Add($"{seconds}秒");
                    }
                }
            }
            else if (hours > 0)
            {
                parts.Add($"{hours}小时");
                if (minutes > 0 || (showSeconds && seconds > 0))
                {
                    parts.Add($"{minutes}分钟");
                    if (showSeconds && seconds > 0)
                    {
                        parts.Add($"{seconds}秒");
                    }
                }
            }
            else if (minutes > 0)
            {
                parts.Add($"{minutes}分钟");
                if (showSeconds && seconds > 0)
                {
                    parts.Add($"{seconds}秒");
                }
            }
            else if (showSeconds && seconds > 0)
            {
                return $"{seconds}秒";
            }
            else
            {
                return showSeconds ? "0秒" : "0分钟";
            }

            return string.Join(" ", parts);
        }

        private void UpdateRemainingCells()
        {
            foreach (ListViewItem row in lv.Items)
            {
                if (row.Tag is TaskItem t)
                    row.SubItems[5].Text = t.RemainingStr; // 剩余列
            }
        }

        // 完成=灰字；删除=删除线；到点=背景浅灰
        private void RepaintStyles()
        {
            foreach (ListViewItem row in lv.Items)
            {
                if (row.Tag is not TaskItem t) continue;

                // 默认
                row.BackColor = Color.White;
                row.ForeColor = Color.Black;
                row.Font = Font;

                // 状态样式
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

                // 按钮文字
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
                case 6: // 完成列
                    t.Done = !t.Done;
                    t.CompletedTime = t.Done ? DateTime.Now : null;
                    SaveTasks();
                    RefreshTable();
                    break;

                case 7: // 删除列
                    t.PendingDelete = !t.PendingDelete;
                    t.DeleteMarkTime = t.PendingDelete ? DateTime.Now : null;
                    SaveTasks();
                    RefreshTable();
                    break;

                case >= 0 and < 6: // 加载选中任务到编辑区
                    // 加载选中任务到编辑区（属于程序更新，不改变自动跟随状态）
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
            if (cbAccount.SelectedItem is string s && !string.IsNullOrWhiteSpace(s))
                return s;
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
                // 编辑现有任务
                tasks[sel] = t;
                if (sortMode == SortMode.DefaultByFinish)
                {
                    var moved = tasks[sel];
                    tasks.RemoveAt(sel);
                    InsertTaskByFinish(moved);
                }
            }
            else
            {
                if (sortMode == SortMode.DefaultByFinish)
                    InsertTaskByFinish(t);
                else
                    tasks.Add(t);
            }

            SaveTasks();
            RefreshTable();
        }

        private void InsertTaskByFinish(TaskItem t)
        {
            int i = 0;
            while (i < tasks.Count && tasks[i].Finish <= t.Finish) i++;
            tasks.Insert(i, t);
        }

        private void SortByFinish()
        {
            var list = new List<TaskItem>(tasks);
            list.Sort((a, b) => a.Finish.CompareTo(b.Finish));
            tasks.Clear();
            foreach (var t in list) tasks.Add(t);
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

                Toast($"[到点] {t.Account}", $"{t.TaskName} 完成时间：{t.FinishStr}");
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
                bool shouldRemove = false;

                // 检查待删除任务
                if (t.PendingDelete)
                {
                    var mark = t.DeleteMarkTime ?? DateTime.MinValue;
                    // 满足“强制清理”或超过延迟秒数
                    if (force || (now - mark).TotalSeconds >= PendingDeleteDelaySeconds)
                    {
                        shouldRemove = true;
                    }
                }

                // 检查已完成超过1分钟的任务
                else if (t is { Done: true, CompletedTime: not null } &&
                         (now - t.CompletedTime.Value).TotalMinutes >= 1)
                {
                    shouldRemove = true;
                }

                if (shouldRemove)
                {
                    tasks.RemoveAt(i);
                    changed = true;
                }
                else
                {
                    i++;
                }
            }

            if (!changed) return;

            // 删除后在默认模式下维持按完成时间排序
            if (sortMode == SortMode.DefaultByFinish) SortByFinish();

            SaveTasks();
            RefreshTable(); // 立刻刷新列表，条目直接消失
        }

        // ---------- 账号/任务管理 ----------
        private void ShowManager(bool isAccount)
        {
            using var dlg = new ManageListForm(isAccount ? "账号管理" : "任务管理",
                isAccount ? settings.Accounts : settings.TaskPresets);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // 更新列表
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

        // ---------- 注册表开机自启 ----------
        private static bool QueryAutostart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (key == null) return false;
                var val = key.GetValue(AppClass) as string;
                return !string.IsNullOrEmpty(val);
            }
            catch
            {
                return false;
            }
        }

        private void ToggleAutostart()
        {
            settings.StartupOnBoot = !settings.StartupOnBoot;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",
                    writable: true);
                if (key != null)
                {
                    if (settings.StartupOnBoot)
                    {
                        string exe = $"\"{Application.ExecutablePath}\"";
                        key.SetValue(AppClass, exe, RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(AppClass, throwOnMissingValue: false);
                    }
                }
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

    // ---------- 简易输入框 ----------
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

    // ---------- 管理窗口（账号/任务预设） ----------
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

            Controls.AddRange(new Control[] { lb, btnAdd, btnDel, btnClose });
        }
    }
}