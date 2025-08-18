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

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game_Upgrade_Reminder.Core.Services;
using Game_Upgrade_Reminder.Infrastructure.Repositories;
using Game_Upgrade_Reminder.Infrastructure.System;
using Game_Upgrade_Reminder.Infrastructure.UI;
using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 主窗体：负责界面布局、用户交互、任务展示与托盘交互。
    /// 主要模块：菜单、顶部账号/任务区、时间区、任务列表、托盘与通知、设置的加载/保存。
    /// </summary>
    public sealed partial class MainForm : Form
    {
        // 常量
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

        // 服务
        private readonly JsonTaskRepository taskRepo = new();
        private readonly JsonSettingsStore settingsStore = new();
        private readonly RegistryAutostartManager autostartManager = new();
        private readonly ByFinishTimeSortStrategy sortStrategy = new();

        private SimpleDeletionPolicy deletionPolicy =
            new(pendingDeleteDelaySeconds: 3, completedKeepSeconds: 60);

        private readonly ZhCnDurationFormatter durationFormatter = new();

        // 状态
        private SettingsData settings = new();
        private readonly BindingList<TaskItem> tasks = [];
        private bool followSystemStartTime;
        private bool isUpdatingStartProgrammatically;
        private bool userEditingStart;

        private enum SortMode
        {
            DefaultByFinish,
            Custom
        }

        /// <summary>
        /// 当列表无选中项时，清除一切焦点痕迹（包括每一项的 Focused 标记与 ListView.FocusedItem），
        /// 并可选择将焦点移出 ListView，避免虚线焦点框。
        /// </summary>
        private void ClearListViewFocusIfNoSelection(bool moveFocusAway = true)
        {
            try
            {
                if (lv.IsDisposed) return;
                if (lv.SelectedIndices.Count > 0) return;

                // 清除每一项的 Focused 以防止系统重绘焦点框
                foreach (ListViewItem it in lv.Items)
                {
                    if (it.Focused) it.Focused = false;
                }

                // 清除 ListView 的焦点项
                lv.FocusedItem = null;

                // 可选地把焦点移到其它控件上
                if (moveFocusAway && dtpStart.CanFocus)
                {
                    ActiveControl = dtpStart;
                }
            }
            catch
            {
                // 忽略异常以保证 UI 流畅
            }
        }

        /// <summary>
        /// 强制清除 ListView 的焦点痕迹（不论是否有选中项），主要用于列头排序等不改变选中状态但会导致焦点框出现的场景。
        /// </summary>
        private void ClearListViewFocusRegardlessOfSelection(bool moveFocusAway = true)
        {
            try
            {
                if (lv.IsDisposed) return;
                foreach (ListViewItem it in lv.Items)
                {
                    if (it.Focused) it.Focused = false;
                }

                lv.FocusedItem = null;
                if (moveFocusAway && dtpStart.CanFocus)
                {
                    ActiveControl = dtpStart;
                }
            }
            catch
            {
                // 忽略
            }
        }

        // ---------- 工具/辅助 ----------
        private void UpdateStatusBar()
        {
            var now = DateTime.Now;
            // 不统计“待删除”的任务
            var total = tasks.Count(t => t is { PendingDelete: false });
            var due = tasks.Count(t => t is { PendingDelete: false, Done: false } && t.Finish <= now);
            var pending = tasks.Count(t => t is { PendingDelete: false, Done: false } && t.Finish > now);

            lblTotal.Text = $"总数: {total}";
            lblDue.Text = $"已到点: {due}";
            lblPending.Text = $"进行中: {pending}";

            // 计算下一个提醒点（包含提前提醒或到点提醒）
            DateTime? next = null;
            int adv = settings.AdvanceNotifySeconds;
            foreach (var t in tasks)
            {
                if (adv > 0 && !t.AdvanceNotified)
                {
                    var advTime = t.Finish.AddSeconds(-adv);
                    if (advTime > now) next = next == null || advTime < next ? advTime : next;
                }

                if (!t.Notified && t.Finish > now && (settings.AlsoNotifyAtDue || !t.AdvanceNotified))
                {
                    next = next == null || t.Finish < next ? t.Finish : next;
                }
            }

            lblNext.Text = next.HasValue ? $@"下一个: {(next.Value - now):hh\:mm\:ss}" : "下一个: -";

            // 同步托盘菜单状态
            UpdateTrayMenuStatus();
        }

        private void UpdateTrayMenuStatus()
        {
            try
            {
                // 从状态栏文本直接同步，保持显示一致
                miStatTotal.Text = lblTotal.Text;
                miStatDue.Text = lblDue.Text;
                miStatPending.Text = lblPending.Text;
                miStatNext.Text = lblNext.Text;
            }
            catch
            {
                // 忽略
            }
        }

        private void AdjustListViewColumns()
        {
            if (lv.Columns.Count < 8) return;

            // 固定列总宽（开始、持续、完成、剩余、操作两列）
            const int fixedSum = StartTimeColWidth + DurationColWidth + FinishTimeColWidth + RemainingTimeColWidth +
                                 (ActionColWidth * 2);
            var available = lv.ClientSize.Width - fixedSum - 8;
            if (available < 100) available = 100;

            // Account 与 Task 两列平均分配，并设置最小值
            const int minA = 100, minT = 120;
            var a = Math.Max(minA, available / 2);
            var t = Math.Max(minT, available - a);

            lv.Columns[0].Width = a; // 账号
            lv.Columns[1].Width = t; // 任务
            lv.Columns[2].Width = StartTimeColWidth;
            lv.Columns[3].Width = DurationColWidth;
            lv.Columns[4].Width = FinishTimeColWidth;
            lv.Columns[5].Width = RemainingTimeColWidth;
            lv.Columns[6].Width = ActionColWidth;
            lv.Columns[7].Width = ActionColWidth;
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            Action? handler = e.KeyData switch
            {
                Keys.F5 => () =>
                {
                    PurgePending(force: true);
                    RefreshTable();
                },
                Keys.Control | Keys.N => () => { btnAddSave.PerformClick(); },
                Keys.Control | Keys.Shift | Keys.D => () => { btnDeleteDone.PerformClick(); },
                Keys.Control | Keys.O => OpenConfigFolder,
                Keys.Control | Keys.U => () => { _ = CheckForUpdatesAsync(this); },
                Keys.Control | Keys.Q => () =>
                {
                    settings.MinimizeOnClose = false;
                    Close();
                },
                _ => null
            };

            if (handler is null) return;

            e.Handled = true;
            handler();
        }

        private static void OpenConfigFolder()
        {
            try
            {
                var folder = AppContext.BaseDirectory;
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开配置文件夹：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        // 字体
        private Font? strikeFont;

        // 控件
        private readonly ComboBox cbAccount = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "账号",
            AccessibleDescription = "选择账号"
        };

        private readonly Button btnAccountMgr = new()
        {
            Text = "账号管理(&M)",
            AccessibleName = "账号管理",
            AccessibleDescription = "打开账号管理"
        };

        private readonly ComboBox cbTask = new()
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AccessibleName = "任务",
            AccessibleDescription = "输入或选择任务"
        };

        private readonly Button btnTaskMgr = new()
        {
            Text = "任务管理(&K)",
            AccessibleName = "任务管理",
            AccessibleDescription = "打开任务管理"
        };

        private readonly Button btnDeleteDone = new()
        {
            Text = "删除已完成(&D)",
            AccessibleName = "删除已完成",
            AccessibleDescription = "删除已完成的任务（Ctrl+Shift+D）"
        };

        private readonly Button btnRefresh = new()
        {
            Text = "刷新(&R)",
            AccessibleName = "刷新",
            AccessibleDescription = "刷新列表（F5）"
        };

        private readonly Button btnNow = new()
        {
            Text = "当前时间(&T)",
            AccessibleName = "当前时间",
            AccessibleDescription = "将开始时间设置为当前时间"
        };

        private readonly NumericUpDown numDays = new()
        {
            Minimum = 0,
            Maximum = 3650,
            Width = 40,
            AccessibleName = "天",
            AccessibleDescription = "持续时间（天）"
        };

        private readonly NumericUpDown numHours = new()
        {
            Minimum = 0,
            Maximum = 1000,
            Width = 40,
            AccessibleName = "小时",
            AccessibleDescription = "持续时间（小时）"
        };

        private readonly NumericUpDown numMinutes = new()
        {
            Minimum = 0,
            Maximum = 59,
            Width = 40,
            AccessibleName = "分钟",
            AccessibleDescription = "持续时间（分钟）"
        };

        private readonly TextBox tbFinish = new()
        {
            ReadOnly = true,
            AccessibleName = "完成时间",
            AccessibleDescription = "根据开始时间与持续时间计算的完成时间"
        };

        private readonly Button btnAddSave = new()
        {
            Text = "添加(&N)",
            AccessibleName = "添加",
            AccessibleDescription = "添加/保存任务（Ctrl+N）"
        };

        // 双缓冲 ListView
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
                AllowDrop = false;
            }
        }

        private readonly ListView lv = new DoubleBufferedListView();

        // 菜单与托盘
        private readonly MenuStrip menu = new();
        private readonly ToolStripMenuItem miSettings = new() { Text = "设置(&S)" };
        private readonly ToolStripMenuItem miFont = new() { Text = "选择字体(&F)..." };
        private readonly ToolStripMenuItem miAutoStart = new() { Text = "开机自启(&A)" };

        // 悬浮显示的三个菜单项
        private readonly ToolStripMenuItem miOpenConfig = new()
        {
            Text = "打开配置文件夹(&O)",
            AccessibleName = "打开配置文件夹",
            AccessibleDescription = "打开配置文件夹（Ctrl+O）"
        };

        private readonly ToolStripMenuItem miAutoDelete = new() { Text = "已完成任务自行删除(&D)" };
        private readonly ToolStripMenuItem miDelOff = new() { Text = "关闭" };
        private readonly ToolStripMenuItem miDel30S = new() { Text = "30秒" };
        private readonly ToolStripMenuItem miDel1M = new() { Text = "1分钟" };
        private readonly ToolStripMenuItem miDel3M = new() { Text = "3分钟" };
        private readonly ToolStripMenuItem miDel30M = new() { Text = "30分钟" };
        private readonly ToolStripMenuItem miDel1H = new() { Text = "1小时" };
        private readonly ToolStripMenuItem miDelCustom = new() { Text = "自定义..." };
        private readonly ToolStripMenuItem miAdvanceNotify = new() { Text = "提前通知(&N)" };
        private readonly ToolStripMenuItem miAdvOff = new() { Text = "关闭" };
        private readonly ToolStripMenuItem miAdvAlsoDue = new() { Text = "同时准点通知" };
        private readonly ToolStripMenuItem miAdv30S = new() { Text = "30秒" };
        private readonly ToolStripMenuItem miAdv1M = new() { Text = "1分钟" };
        private readonly ToolStripMenuItem miAdv3M = new() { Text = "3分钟" };
        private readonly ToolStripMenuItem miAdv30M = new() { Text = "30分钟" };
        private readonly ToolStripMenuItem miAdv1H = new() { Text = "1小时" };
        private readonly ToolStripMenuItem miAdvCustom = new() { Text = "自定义..." };
        private readonly ToolStripMenuItem miCloseBehavior = new() { Text = "关闭按钮行为(&C)" };
        private readonly ToolStripMenuItem miCloseExit = new() { Text = "退出程序" };
        private readonly ToolStripMenuItem miCloseMinimize = new() { Text = "最小化到托盘" };
        private readonly ToolStripMenuItem miAboutTop = new() { Text = "关于(&A)..." };

        private readonly NotifyIcon tray = new();
        private readonly ContextMenuStrip trayMenu = new();
        private readonly TrayNotifier notifier;

        // 托盘状态项
        private readonly ToolStripMenuItem miStatHeader = new() { Text = "状态", Enabled = false };
        private readonly ToolStripMenuItem miStatTotal = new() { Text = "总数: 0", Enabled = false };
        private readonly ToolStripMenuItem miStatDue = new() { Text = "已到点: 0", Enabled = false };
        private readonly ToolStripMenuItem miStatPending = new() { Text = "进行中: 0", Enabled = false };
        private readonly ToolStripMenuItem miStatNext = new() { Text = "下一个: -", Enabled = false };

        // 底部状态栏
        private readonly StatusStrip status = new();
        private readonly ToolStripStatusLabel lblTotal = new() { Text = "总数: 0" };
        private readonly ToolStripStatusLabel lblDue = new() { Text = "已到点: 0" };
        private readonly ToolStripStatusLabel lblPending = new() { Text = "进行中: 0" };
        private readonly ToolStripStatusLabel lblNext = new() { Text = "下一个: -" };

        // 计时器
        private readonly System.Windows.Forms.Timer timerTick = new() { Interval = 1_000 };
        private readonly System.Windows.Forms.Timer timerUi = new() { Interval = 1000 };

        private readonly System.Windows.Forms.Timer timerPurge = new() { Interval = 500 };

        // 菜单悬浮自动关闭控制
        private readonly System.Windows.Forms.Timer hoverMenuTimer = new() { Interval = 200 };
        private ToolStripMenuItem? hoverPendingClose;

        // ---------- 原生 Header 箭头（与系统主题同步） ----------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct Hditem
        {
            public uint mask;
            public int cxy;
            public IntPtr pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public IntPtr lParam;
            public int iImage;
            public int iOrder;
            public uint type;
            public IntPtr pvFilter;
            public uint state;
        }

        private const int LvmFirst = 0x1000;
        private const int LvmGetHeader = LvmFirst + 31;
        private const int HdmFirst = 0x1200;
        private const int HdmGetItem = HdmFirst + 11;
        private const int HdmSetItem = HdmFirst + 12;

        private const int HdiFormat = 0x0004;
        private const int HdfSortUp = 0x0400;
        private const int HdfSortDown = 0x0200;

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial void SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Hditem lParam);

        private void UpdateListViewSortArrow()
        {
            if (lv.IsDisposed || lv.Handle == IntPtr.Zero || lv.Columns.Count == 0) return;
            var header = SendMessage(lv.Handle, LvmGetHeader, IntPtr.Zero, IntPtr.Zero);
            if (header == IntPtr.Zero) return;

            // 计算当前应显示箭头的列与方向：
            // - 自定义排序：使用 customSortColumn/customSortAsc
            // - 默认排序：使用 剩余时间列(索引5) 升序
            var sortedColumn = -1;
            var sortedAsc = true;
            if (sortMode == SortMode.Custom)
            {
                // 仅允许 0..5 这些“可排序列”显示自定义排序箭头；6/7（完成/删除）不显示
                if (customSortColumn is >= 0 and <= 5)
                {
                    sortedColumn = customSortColumn;
                    sortedAsc = customSortAsc;
                }
                else
                {
                    sortedColumn = -1;
                }
            }
            else // SortMode.DefaultByFinish
            {
                if (lv.Columns.Count > 5)
                {
                    sortedColumn = 5; // 剩余时间列
                    sortedAsc = true; // 升序
                }
            }

            for (int i = 0; i < lv.Columns.Count; i++)
            {
                var item = new Hditem { mask = HdiFormat };
                SendMessage(header, HdmGetItem, new IntPtr(i), ref item);

                // 清理旧的箭头位
                item.fmt &= ~(HdfSortUp | HdfSortDown);

                if (i == sortedColumn)
                {
                    item.fmt |= sortedAsc ? HdfSortUp : HdfSortDown;
                }

                SendMessage(header, HdmSetItem, new IntPtr(i), ref item);
            }
        }

        public MainForm()
        {
            Text = AppTitle;
            var iconPath = Path.Combine(AppContext.BaseDirectory, "YuanXi.ico");
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

            notifier = new TrayNotifier(tray);

            BuildMenu();
            BuildUi();
            WireEvents();

            // 加载设置与任务
            LoadSettings();
            ApplyDeletionPolicyFromSettings();
            ApplySettingsToUi();
            RestoreWindowBoundsFromSettings();
            LoadTasks();
            if (sortMode == SortMode.DefaultByFinish) sortStrategy.Sort(tasks);
            RefreshTable();

            // 同步自启动状态
            var actuallyOn = autostartManager.IsEnabled();
            if (actuallyOn != settings.StartupOnBoot)
            {
                settings.StartupOnBoot = actuallyOn;
                SaveSettings();
                UpdateMenuChecks();
            }

            // 启动计时器
            timerTick.Start();
            timerUi.Start();
            timerPurge.Start();

            // 初始开始时间跟随系统时间
            isUpdatingStartProgrammatically = true;
            dtpStart.Value = DateTime.Now;
            isUpdatingStartProgrammatically = false;
            followSystemStartTime = true;
            RecalcFinishFromFields();

            // 托盘初始化
            InitTray();

            // 初始调度（根据最近的提醒点设置计时器间隔）
            RescheduleNextTick();

            // 首次显示时：同步列头箭头，并把焦点移出列表，去掉默认的虚线焦点框
            Shown += (_, _) =>
            {
                UpdateListViewSortArrow();
                if (dtpStart.CanFocus) ActiveControl = dtpStart;
                ClearListViewFocusIfNoSelection();
            };
        }

        // ---------- 菜单 / 界面 ----------
        /// <summary>
        /// 构建主菜单与设置项，包括字体、开机自启、自动删除、提前通知与关闭按钮行为等。
        /// </summary>
        private void BuildMenu()
        {
            miCloseBehavior.DropDownItems.AddRange([miCloseExit, miCloseMinimize]);

            // 已完成任务自动删除 预设
            miAutoDelete.DropDownItems.AddRange([
                miDelOff,
                new ToolStripSeparator(),
                miDel30S,
                miDel1M,
                miDel3M,
                miDel30M,
                miDel1H,
                new ToolStripSeparator(),
                miDelCustom
            ]);

            // 提前通知预设
            miAdvanceNotify.DropDownItems.AddRange([
                miAdvOff,
                miAdvAlsoDue,
                new ToolStripSeparator(),
                miAdv30S,
                miAdv1M,
                miAdv3M,
                miAdv30M,
                miAdv1H,
                new ToolStripSeparator(),
                miAdvCustom
            ]);

            miSettings.DropDownItems.Add(miFont);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(miAutoStart);
            miSettings.DropDownItems.Add(miOpenConfig);
            miSettings.DropDownItems.Add(miAutoDelete);
            miSettings.DropDownItems.Add(miAdvanceNotify);
            miSettings.DropDownItems.Add(new ToolStripSeparator());
            miSettings.DropDownItems.Add(miCloseBehavior);

            menu.Items.Add(miSettings);
            miAboutTop.Alignment = ToolStripItemAlignment.Left;
            menu.Items.Add(miAboutTop);

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        /// <summary>
        /// 为需要“悬浮显示/离开自动关闭”的菜单项设置统一的事件处理。
        /// </summary>
        private void SetupHoverDropdown(ToolStripMenuItem mi)
        {
            // 鼠标进入：显示此下拉并关闭其他两个，取消待关闭
            mi.MouseEnter += (_, _) =>
            {
                CloseOtherHoverMenus(mi);
                if (!mi.DropDown.Visible) mi.ShowDropDown();
                hoverPendingClose = null;
                hoverMenuTimer.Stop();
            };

            // 鼠标离开：启动一个短延时检测，若鼠标不在此项或其下拉上则关闭
            mi.MouseLeave += (_, _) =>
            {
                hoverPendingClose = mi;
                hoverMenuTimer.Stop();
                hoverMenuTimer.Start();
            };

            // 移动到下拉时，取消待关闭；离开下拉后开始检测
            mi.DropDown.MouseEnter += (_, _) =>
            {
                if (hoverPendingClose == mi)
                {
                    hoverPendingClose = null;
                    hoverMenuTimer.Stop();
                }
            };
            mi.DropDown.MouseLeave += (_, _) =>
            {
                hoverPendingClose = mi;
                hoverMenuTimer.Stop();
                hoverMenuTimer.Start();
            };
        }

        /// <summary>
        /// 关闭除 current 之外的其他“悬浮显示”菜单，以避免多个下拉同时可见。
        /// </summary>
        private void CloseOtherHoverMenus(ToolStripMenuItem current)
        {
            foreach (var other in new[] { miAutoDelete, miAdvanceNotify, miCloseBehavior })
            {
                if (other != current && other.DropDown.Visible) other.HideDropDown();
            }
        }

        /// <summary>
        /// 判断鼠标是否位于给定菜单项（顶层）区域内。
        /// </summary>
        private static bool IsMouseOverMenuItem(ToolStripMenuItem mi, Point mouseScreenPoint)
        {
            var owner = mi.Owner;
            if (owner is null) return false;
            var origin = owner.PointToScreen(mi.Bounds.Location);
            var rect = new Rectangle(origin, mi.Bounds.Size);
            return rect.Contains(mouseScreenPoint);
        }

        /// <summary>
        /// 构建主界面布局与控件结构（不含业务逻辑）。
        /// 包含：账号/任务区、时间与持续时长区、任务列表区。
        /// </summary>
        private void BuildUi()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(3);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            // 第 1 行（账号与任务）
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
            btnAccountMgr.Text = "管理账号(&M)";
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

            btnTaskMgr.Text = "管理任务(&K)";
            btnTaskMgr.AutoSize = true;
            btnTaskMgr.Anchor = AnchorStyles.Left;
            btnTaskMgr.Margin = new Padding(0, 2, 0, 2);
            line1.Controls.Add(btnTaskMgr, 6, 0);

            btnDeleteDone.Text = "删除已完成(&D)";
            btnRefresh.Text = "刷新(&R)";
            StyleSmallButton(btnDeleteDone);
            StyleSmallButton(btnRefresh);

            line1.Controls.Add(btnDeleteDone, 8, 0);
            line1.Controls.Add(btnRefresh, 9, 0);

            gbTop.Controls.Add(line1);
            root.Controls.Add(gbTop, 0, 0);

            // 第 2 行（时间与持续时长）
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

            btnNow.Text = "当前时间(&T)";
            btnNow.AutoSize = true;
            btnNow.Anchor = AnchorStyles.Left;
            btnNow.Margin = new Padding(0, 0, 0, 0);
            line2.Controls.Add(btnNow, 2, 0);

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

            line2.Controls.Add(MakeAutoLabel("完成时间"), 10, 0);
            tbFinish.ReadOnly = true;
            tbFinish.Margin = new Padding(0, 2, 6, 2);
            tbFinish.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            line2.Controls.Add(tbFinish, 11, 0);

            btnAddSave.Text = "添加(&N)";
            StyleSmallButton(btnAddSave, new Padding(0, 2, 0, 2));
            line2.Controls.Add(btnAddSave, 12, 0);

            gbTime.Controls.Add(line2);
            root.Controls.Add(gbTime, 0, 1);

            // 第 3 行（任务列表）
            lv.Dock = DockStyle.Fill;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.View = View.Details;
            root.Controls.Add(lv, 0, 2);

            InitListViewColumns();
            AdjustListViewColumns();

            // 底部状态栏
            status.SizingGrip = true;
            status.Items.Add(lblTotal);
            status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            status.Items.Add(lblDue);
            status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            status.Items.Add(lblPending);
            status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            status.Items.Add(lblNext);
            root.Controls.Add(status, 0, 3);
        }

        /// <summary>
        /// 初始化任务列表列头与列宽，确保与常量宽度保持一致。
        /// </summary>
        private void InitListViewColumns()
        {
            lv.Columns.Clear();
            lv.Columns.AddRange(
            [
                new ColumnHeader { Text = "账号", Width = AccountColWidth },
                new ColumnHeader { Text = "任务", Width = TaskColWidth },
                new ColumnHeader { Text = "开始时间", Width = StartTimeColWidth },
                new ColumnHeader { Text = "持续时间", Width = DurationColWidth },
                new ColumnHeader { Text = "完成时间", Width = FinishTimeColWidth },
                new ColumnHeader { Text = "剩余时间", Width = RemainingTimeColWidth },
                new ColumnHeader { Text = "完成", Width = ActionColWidth },
                new ColumnHeader { Text = "删除", Width = ActionColWidth }
            ]);
        }

        /// <summary>
        /// 生成自动尺寸的标签，常用于表单左侧的说明文字。
        /// </summary>
        private static Label MakeAutoLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 3, 6, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left
        };

        /// <summary>
        /// 统一绑定菜单、输入控件、列表、计时器等事件处理。
        /// 仅连接事件与状态更新，不包含业务数据加载。
        /// </summary>
        private void WireEvents()
        {
            // 菜单事件
            miFont.Click += (_, _) => DoChooseFont();
            miAutoStart.Click += (_, _) => ToggleAutostart();
            miOpenConfig.Click += (_, _) => OpenConfigFolder();
            // 自动删除下拉
            miAutoDelete.DropDownOpening += (_, _) => UpdateAutoDeleteMenuChecks();
            miDelOff.Click += (_, _) => SetAutoDeleteSecondsAndSave(0);
            miDel30S.Click += (_, _) => SetAutoDeleteSecondsAndSave(30);
            miDel1M.Click += (_, _) => SetAutoDeleteSecondsAndSave(60);
            miDel3M.Click += (_, _) => SetAutoDeleteSecondsAndSave(180);
            miDel30M.Click += (_, _) => SetAutoDeleteSecondsAndSave(1800);
            miDel1H.Click += (_, _) => SetAutoDeleteSecondsAndSave(3600);
            miDelCustom.Click += (_, _) =>
            {
                using var dlg = new AdvanceTimeDialog(settings.AutoDeleteCompletedSeconds);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetAutoDeleteSecondsAndSave(dlg.TotalSeconds);
                }
            };
            miAdvanceNotify.DropDownOpening += (_, _) => UpdateAdvanceMenuChecks();
            miAdvOff.Click += (_, _) => SetAdvanceSecondsAndSave(0);
            miAdv30S.Click += (_, _) => SetAdvanceSecondsAndSave(30);
            miAdv1M.Click += (_, _) => SetAdvanceSecondsAndSave(60);
            miAdv3M.Click += (_, _) => SetAdvanceSecondsAndSave(180);
            miAdv30M.Click += (_, _) => SetAdvanceSecondsAndSave(1800);
            miAdv1H.Click += (_, _) => SetAdvanceSecondsAndSave(3600);
            miAdvAlsoDue.Click += (_, _) =>
            {
                settings.AlsoNotifyAtDue = !settings.AlsoNotifyAtDue;
                SaveSettings();
                UpdateAdvanceMenuChecks();
                RescheduleNextTick();
            };
            miAdvCustom.Click += (_, _) =>
            {
                using var dlg = new AdvanceTimeDialog(settings.AdvanceNotifySeconds);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetAdvanceSecondsAndSave(dlg.TotalSeconds);
                }
            };
            // 悬浮显示/自动关闭：统一设置
            SetupHoverDropdown(miAutoDelete);
            SetupHoverDropdown(miAdvanceNotify);
            SetupHoverDropdown(miCloseBehavior);
            hoverMenuTimer.Tick += (_, _) =>
            {
                if (hoverPendingClose is null)
                {
                    hoverMenuTimer.Stop();
                    return;
                }

                var mi = hoverPendingClose;
                var pt = Control.MousePosition;
                // 若鼠标不在菜单项或其下拉范围内，则关闭
                bool overItem = IsMouseOverMenuItem(mi, pt);
                bool overDrop = mi.DropDown.Visible && mi.DropDown.Bounds.Contains(pt);
                if (!overItem && !overDrop)
                {
                    mi.HideDropDown();
                    hoverPendingClose = null;
                    hoverMenuTimer.Stop();
                }
            };
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
            miAboutTop.Click += (_, _) => ShowAboutDialog();

            // 开始时间编辑检测
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

            // 测试按钮（保留）
            var btnTest = new Button { Text = "测试排序", Location = new Point(10, 10), AutoSize = true };
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

            // 列表行为
            lv.ItemActivate += (_, _) => HandleListClick();
            lv.MouseUp += (_, me) =>
            {
                if (me.Button == MouseButtons.Left) HandleListClick();
            };
            lv.Resize += (_, _) => AdjustListViewColumns();
            lv.GotFocus += (_, _) =>
            {
                // 当列表获得焦点但没有选中项时，清除焦点项避免虚线焦点框
                ClearListViewFocusIfNoSelection();
            };

            // 列头点击排序（文本为主，部分列有特殊处理）
            lv.ColumnClick += (_, e) =>
            {
                var column = e.Column;
                // 完成(6)/删除(7) 列：不参与排序，也不显示箭头
                if (column >= 6) return;

                sortMode = SortMode.Custom;
                var sortAscending = column != customSortColumn || !customSortAsc;

                lv.ListViewItemSorter = new ListViewItemComparer(column, sortAscending);
                lv.Sort();

                customSortColumn = column;
                customSortAsc = sortAscending;

                var newOrder = new List<TaskItem>();
                foreach (ListViewItem row in lv.Items)
                    if (row.Tag is TaskItem tsk)
                        newOrder.Add(tsk);

                tasks.Clear();
                foreach (var t in newOrder) tasks.Add(t);

                SaveTasks();
                UpdateListViewSortArrow();

                // 异步移除虚线焦点框并移走焦点，确保在排序后的消息循环稳定后处理
                BeginInvoke(() =>
                {
                    if (lv.SelectedIndices.Count != 0) return;
                    ClearListViewFocusRegardlessOfSelection();
                    if (dtpStart.CanFocus) dtpStart.Focus();
                });
            };

            // 计时器
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

            // 关闭按钮 -> 最小化逻辑
            FormClosing += MainForm_FormClosing;

            // 窗口尺寸/位置变更时记忆
            LocationChanged += (_, _) => UpdateWindowBoundsToSettings(save: false);
            SizeChanged += (_, _) => UpdateWindowBoundsToSettings(save: false);
            Resize += (_, _) => AdjustListViewColumns();

            // 快捷键
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        // ---------- 关于对话框（可点击链接 + 检查更新按钮） ----------
        /// <summary>
        /// 显示“关于”对话框：包含版本信息、版权与许可证、项目链接，并提供“检查更新”。
        /// 使用主窗体作为更新检查的宿主，避免捕获临时对话框实例。
        /// </summary>
        private void ShowAboutDialog()
        {
            const string appDisplayName = "游戏升级提醒";
            const string gitHubUrl = "https://github.com/YuanXiQWQ/Game-Upgrade-Reminder";
            const string licenseUrl = "https://www.gnu.org/licenses/agpl-3.0.html";

            var ver = GetCurrentVersion();
            var versionText = $"版本 v{ver.Major}.{ver.Minor}.{ver.Build}" + (ver.Revision > 0 ? $".{ver.Revision}" : "");

            using var dlg = new Form();
            dlg.Text = "关于";
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MinimizeBox = false;
            dlg.MaximizeBox = false;
            dlg.ShowInTaskbar = false;
            dlg.AutoScaleMode = AutoScaleMode.Dpi;
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.MinimumSize = new Size(640, 420);
            dlg.BackColor = SystemColors.Window;

            // ===== 外层：内容区 + 底部按钮条 =====
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                AutoScroll = true
            };
            var buttonBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 8, 16, 16),
                BackColor = SystemColors.Window
            };
            dlg.Controls.Add(contentHost);
            dlg.Controls.Add(buttonBar);

            // ===== 内容容器：单列 TableLayout，避免 Dock 顺序问题 =====
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Top, // 放在可滚动面板顶部
                AutoSize = true, // 根据子控件自然增高
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = SystemColors.Window
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contentHost.Controls.Add(content);

            // ===== 1) 头部：左图标 + 右标题/副标题 + 分隔线 =====
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pic = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(64, 64),
                Margin = new Padding(0, 0, 16, 0),
                Image = (Icon ?? SystemIcons.Information).ToBitmap()
            };
            header.Controls.Add(pic, 0, 0);

            var headerRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1
            };
            var titleLabel = new Label
            {
                AutoSize = true,
                Text = appDisplayName,
                Font = new Font(SystemFonts.CaptionFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 16f,
                    FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };
            var subLabel = new Label
            {
                AutoSize = true,
                Text = "游戏升级计时与提醒工具（说不定还能拿来干点其它的:>）",
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 8)
            };
            var sepHeader = new Panel
            {
                Height = 1, Dock = DockStyle.Top, BackColor = SystemColors.ControlLight,
                Margin = new Padding(0, 4, 0, 0)
            };
            headerRight.Controls.Add(titleLabel);
            headerRight.Controls.Add(subLabel);
            headerRight.Controls.Add(sepHeader);

            header.Controls.Add(headerRight, 1, 0);
            content.Controls.Add(header);

            // ===== 2) 信息卡片：版本/版权/项目/许可证 =====
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = Color.FromArgb(248, 248, 248),
                Padding = new Padding(12),
                Margin = new Padding(0, 12, 0, 0)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));


            card.Controls.Add(CreateMetaLabel("版本"));
            card.Controls.Add(CreateMetaValue(versionText));

            card.Controls.Add(CreateMetaLabel("版权"));
            card.Controls.Add(CreateMetaValue("© 2025 YuanXiQWQ  •  AGPL-3.0"));

            card.Controls.Add(CreateMetaLabel("许可证"));
            var linkLicense = new LinkLabel
            {
                AutoSize = true,
                Text = "GNU AGPL-3.0",
                Margin = new Padding(0, 6, 0, 6)
            };
            linkLicense.Links.Add(0, linkLicense.Text.Length, licenseUrl);
            linkLicense.LinkClicked += (_, e) =>
            {
                var url = e.Link?.LinkData?.ToString();
                if (string.IsNullOrEmpty(url)) return;

                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法打开链接: {ex.Message}");
                }
            };
            card.Controls.Add(linkLicense);

            content.Controls.Add(card); // 第 1 行

            // ===== 3) 底部按钮条：左两右一 =====
            var btnGitHub = new Button { AutoSize = true, Text = "打开 GitHub" };
            btnGitHub.Click += (_, _) => Process.Start(new ProcessStartInfo(gitHubUrl) { UseShellExecute = true });

            var btnUpdate = new Button { AutoSize = true, Text = "检查更新" };
            btnUpdate.Click += async (_, _) => await CheckForUpdatesAsync(this);

            var btnClose = new Button { AutoSize = true, Text = "关闭", DialogResult = DialogResult.OK };

            var btnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 4
            };
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            btnLayout.Controls.Add(btnGitHub, 0, 0);
            btnLayout.Controls.Add(btnUpdate, 1, 0);
            btnLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }, 2, 0);
            btnLayout.Controls.Add(btnClose, 3, 0);

            buttonBar.Controls.Add(btnLayout);

            dlg.AcceptButton = btnClose;
            dlg.CancelButton = btnClose;

            dlg.ShowDialog(this);
        }

        /// <summary>
        /// 生成“元信息”说明标签（灰色文本），用于信息卡片左侧说明列。
        /// </summary>
        private static Label CreateMetaLabel(string t) => new Label
        {
            AutoSize = true,
            Text = t,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 6, 12, 6)
        };

        /// <summary>
        /// 生成“值”标签（常规文本），用于信息卡片右侧具体内容。
        /// </summary>
        private static Label CreateMetaValue(string t) => new Label
        {
            AutoSize = true,
            Text = t,
            Margin = new Padding(0, 6, 0, 6)
        };

        /// <summary>
        /// 统一小按钮外观（尺寸、边距、对齐），保持 UI 风格一致。
        /// </summary>
        private static void StyleSmallButton(Button b, Padding? margin = null)
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
                // 保存窗口位置与尺寸
                UpdateWindowBoundsToSettings(save: true);
                SaveTasks();
                SaveSettings();
                tray.Visible = false;
            }
        }

        // 比较器
        private class ListViewItemComparer(int col, bool asc = true) : System.Collections.IComparer
        {
            public int Compare(object? x, object? y) => (x, y) switch
            {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => 1,
                _ => CompareItems(x, y)
            };

            private int CompareItems(object x, object y)
            {
                if (x is not ListViewItem lvx || y is not ListViewItem lvy) return 0;

                var xText = col < lvx.SubItems.Count ? lvx.SubItems[col].Text : string.Empty;
                var yText = col < lvy.SubItems.Count ? lvy.SubItems[col].Text : string.Empty;

                switch (col)
                {
                    // 日期列
                    case 2 or 4 when DateTime.TryParse(xText, out var xDate) && DateTime.TryParse(yText, out var yDate):
                        return asc ? xDate.CompareTo(yDate) : yDate.CompareTo(xDate);
                    // 持续时间列
                    case 3 or 5:
                    {
                        var xSecs = ParseTimeSpanToSeconds(xText);
                        var ySecs = ParseTimeSpanToSeconds(yText);
                        return asc ? xSecs.CompareTo(ySecs) : ySecs.CompareTo(xSecs);
                    }
                }

                var result = string.Compare(xText, yText, StringComparison.Ordinal);

                // 次级排序：按剩余时间升序
                if (result != 0 || col == 5) return asc ? result : -result;

                var xRemaining = ParseTimeSpanToSeconds(lvx.SubItems[5].Text);
                var yRemaining = ParseTimeSpanToSeconds(lvy.SubItems[5].Text);
                return xRemaining.CompareTo(yRemaining);
            }

            private static int ParseTimeSpanToSeconds(string text)
            {
                if (string.IsNullOrEmpty(text)) return 0;
                var totalSeconds = 0;
                var numberStr = new StringBuilder();

                for (var i = 0; i < text.Length; i++)
                {
                    if (char.IsDigit(text[i])) numberStr.Append(text[i]);
                    else if (numberStr.Length > 0)
                    {
                        var value = int.Parse(numberStr.ToString());
                        numberStr.Clear();

                        var (multiplier, skipNext) = (text[i], i < text.Length - 1 ? text[i + 1] : '\0') switch
                        {
                            ('天', _) => (value * 24 * 3600, 0),
                            ('小', '时') => (value * 3600, 1),
                            ('分', '钟') => (value * 60, 1),
                            ('时', _) => (value * 3600, 0),
                            ('分', _) => (value * 60, 0),
                            ('秒', _) => (value, 0),
                            _ => (0, 0)
                        };
                        totalSeconds += multiplier;
                        i += skipNext;
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

        // ---------- 设置与持久化 ----------
        private void LoadSettings()
        {
            settings = settingsStore.Load();

            // 首次运行：从注册表读取自启动状态并写入默认设置文件
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "settings.json")))
            {
                settings.StartupOnBoot = autostartManager.IsEnabled();
                SaveSettings();
            }

            if (settings is { AutoDeleteCompletedSeconds: <= 0, AutoDeleteCompletedAfter1Min: true })
            {
                settings.AutoDeleteCompletedSeconds = 60;
                SaveSettings();
            }

            // 合法性：不可为负
            if (settings.AutoDeleteCompletedSeconds < 0)
            {
                settings.AutoDeleteCompletedSeconds = 0;
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

        // ---------- 应用设置到界面 ----------
        /// <summary>
        /// 将应用设置同步到界面控件（账号、任务预设、字体等）。
        /// 在字体应用过程中若出现异常将忽略，确保主界面可继续显示。
        /// </summary>
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
            catch
            {
                /* ignore */
            }
        }

        private void UpdateMenuChecks()
        {
            miAutoStart.Checked = settings.StartupOnBoot;
            miCloseExit.Checked = !settings.MinimizeOnClose;
            miCloseMinimize.Checked = settings.MinimizeOnClose;
            UpdateAutoDeleteMenuChecks();
            UpdateAdvanceMenuChecks();
        }

        /// <summary>
        /// 根据当前设置更新“提前通知”菜单的勾选状态，并同步“同时准点通知”。
        /// </summary>
        private void UpdateAdvanceMenuChecks()
        {
            var secs = settings.AdvanceNotifySeconds;
            var presets = new Dictionary<ToolStripMenuItem, int>
            {
                { miAdvOff, 0 },
                { miAdv30S, 30 },
                { miAdv1M, 60 },
                { miAdv3M, 180 },
                { miAdv30M, 1800 },
                { miAdv1H, 3600 }
            };

            foreach (var kv in presets)
                kv.Key.Checked = kv.Value == secs;

            // 自定义：当为正且不在预设中时勾选
            miAdvCustom.Checked = secs > 0 && !presets.Values.Contains(secs);
            miAdvAlsoDue.Checked = settings.AlsoNotifyAtDue;
        }

        private void SetAdvanceSecondsAndSave(int secs)
        {
            if (secs < 0) secs = 0;
            settings.AdvanceNotifySeconds = secs;
            SaveSettings();
            UpdateAdvanceMenuChecks();
            // 先立即检查一次，再重新计算下次触发，防止临近提醒点被错过
            CheckDueAndNotify();
            RescheduleNextTick();
        }

        /// <summary>
        /// 根据当前设置更新“已完成任务自动删除”菜单勾选状态。
        /// </summary>
        private void UpdateAutoDeleteMenuChecks()
        {
            var secs = settings.AutoDeleteCompletedSeconds;
            var presets = new Dictionary<ToolStripMenuItem, int>
            {
                { miDelOff, 0 },
                { miDel30S, 30 },
                { miDel1M, 60 },
                { miDel3M, 180 },
                { miDel30M, 1800 },
                { miDel1H, 3600 }
            };

            foreach (var kv in presets)
                kv.Key.Checked = kv.Value == secs;

            miDelCustom.Checked = secs > 0 && !presets.Values.Contains(secs);
        }

        private void SetAutoDeleteSecondsAndSave(int secs)
        {
            if (secs < 0) secs = 0;
            settings.AutoDeleteCompletedSeconds = secs;
            ApplyDeletionPolicyFromSettings();
            SaveSettings();
            UpdateAutoDeleteMenuChecks();
            // 变更后立即进行一次清理尝试（非强制），以尽快反映设置
            PurgePending(force: false);
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
                // 忽略
            }

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            ApplyUiFont(dlg.Font);
            settings.UiFont = FontSpec.From(dlg.Font);
            SaveSettings();
        }

        // ---------- 托盘 ----------
        /// <summary>
        /// 初始化系统托盘图标与右键菜单，提供打开与退出操作，并处理双击恢复窗口。
        /// </summary>
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

            // 菜单样式优化
            trayMenu.ShowImageMargin = false;
            trayMenu.RenderMode = ToolStripRenderMode.System;

            // 状态区（只读项）
            miStatHeader.Font = new Font(Font, FontStyle.Bold);
            trayMenu.Items.Add(miStatHeader);
            trayMenu.Items.Add(miStatTotal);
            trayMenu.Items.Add(miStatDue);
            trayMenu.Items.Add(miStatPending);
            trayMenu.Items.Add(miStatNext);
            trayMenu.Items.Add(new ToolStripSeparator());

            trayMenu.Items.Add("打开(&O)", null, (_, _) =>
            {
                Show();
                Activate();
            });
            trayMenu.Items.Add("刷新(&R)", null, (_, _) =>
            {
                PurgePending(force: true);
                RefreshTable();
            });
            trayMenu.Items.Add("检查更新(&U)", null, (_, _) => { _ = CheckForUpdatesAsync(this, openOnNew: true); });
            trayMenu.Items.Add("打开配置文件夹(&F)", null, (_, _) => OpenConfigFolder());
            trayMenu.Items.Add("退出(&X)", null, (_, _) =>
            {
                settings.MinimizeOnClose = false;
                Close();
            });
            tray.ContextMenuStrip = trayMenu;

            UpdateTrayMenuStatus();
        }

        // ---------- 列表 ----------
        /// <summary>
        /// 刷新任务列表内容：根据内存中的 <see cref="tasks"/> 重建 <see cref="lv"/> 的行与样式。
        /// </summary>
        private void RefreshTable()
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            foreach (var t in tasks)
            {
                var duration = durationFormatter.Format(t.Days, t.Hours, t.Minutes);
                var it = new ListViewItem(t.Account)
                {
                    SubItems =
                    {
                        t.TaskName,
                        t.StartStr,
                        duration,
                        t.FinishStr,
                        t.RemainingStr,
                        t.Done ? "撤销完成" : "完成",
                        t.PendingDelete ? "撤销删除" : "删除"
                    },
                    Tag = t
                };
                lv.Items.Add(it);
            }

            RepaintStyles();
            lv.EndUpdate();

            if (sortMode == SortMode.Custom)
            {
                lv.ListViewItemSorter = new ListViewItemComparer(customSortColumn, customSortAsc);
                lv.Sort();
            }

            AdjustListViewColumns();
            UpdateStatusBar();
            UpdateListViewSortArrow();

            // 异步清除焦点项，避免在刷新/排序后出现虚线焦点框
            if (IsHandleCreated)
            {
                BeginInvoke(() =>
                {
                    if (lv.SelectedIndices.Count != 0) return;
                    ClearListViewFocusRegardlessOfSelection();
                    // 若列表当前仍持有焦点，则把焦点移出以彻底避免焦点框
                    if (lv.Focused && dtpStart.CanFocus)
                        dtpStart.Focus();
                });
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

            UpdateStatusBar();
        }

        private void HandleListClick()
        {
            var hit = lv.PointToClient(MousePosition);
            var info = lv.HitTest(hit);
            if (info.Item == null) return;
            var sub = info.Item.SubItems.IndexOf(info.SubItem);

            if (info.Item.Tag is not TaskItem t) return;

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

        // ---------- 新增任务 ----------
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
                AdvanceNotified = false,
                Done = false,
                PendingDelete = false,
                DeleteMarkTime = null
            };

            // 根据选中行的 Tag 精确定位要更新的任务，避免索引与显示顺序不一致导致的错位
            var selectedTask = lv.SelectedItems.Count > 0 ? lv.SelectedItems[0].Tag as TaskItem : null;
            if (selectedTask != null)
            {
                var idx = tasks.IndexOf(selectedTask);
                if (idx >= 0)
                {
                    tasks[idx] = t;
                    if (sortMode == SortMode.DefaultByFinish)
                    {
                        var moved = tasks[idx];
                        tasks.RemoveAt(idx);
                        sortStrategy.Insert(tasks, moved);
                    }
                }
            }
            else
            {
                if (sortMode == SortMode.DefaultByFinish) sortStrategy.Insert(tasks, t);
                else tasks.Add(t);
            }

            SaveTasks();
            RefreshTable();
            UpdateStatusBar();
        }

        private void DeleteAllDone()
        {
            for (var i = 0; i < tasks.Count;)
            {
                if (tasks[i].Done) tasks.RemoveAt(i);
                else i++;
            }
        }

        // ---------- 到点提醒 ----------
        private void CheckDueAndNotify()
        {
            var changed = false;
            var now = DateTime.Now;
            foreach (var t in tasks)
            {
                // Advance notify
                var adv = settings.AdvanceNotifySeconds;
                if (adv > 0 && !t.AdvanceNotified && t.Finish > now)
                {
                    var advTime = t.Finish.AddSeconds(-adv);
                    if (advTime <= now)
                    {
                        notifier.Toast($"[提前] {t.Account}", $"{t.TaskName} 即将到点，完成时间：{t.FinishStr}");
                        t.AdvanceNotified = true;
                        changed = true;
                    }
                }

                // 到点提醒
                if (t.Finish > now || t.Notified) continue;

                // 若用户关闭“同时准点通知”，且已进行过提前通知，则此处直接标记已到点通知，避免再次弹窗
                if (!settings.AlsoNotifyAtDue && t.AdvanceNotified)
                {
                    t.Notified = true;
                    changed = true;
                    continue;
                }

                notifier.Toast($"[到点] {t.Account}", $"{t.TaskName} 完成时间：{t.FinishStr}");
                t.Notified = true;
                changed = true;
            }

            if (changed) SaveTasks();
            RescheduleNextTick();
            UpdateStatusBar();
        }

        // 自适应调度：带“提前量(guard)”与“上下限夹紧”的计时器间隔计算
        private void RescheduleNextTick()
        {
            try
            {
                var now = DateTime.Now;
                DateTime? next = null;
                var adv = settings.AdvanceNotifySeconds;

                foreach (var t in tasks)
                {
                    // 候选1：提前提醒时间点（若启用且尚未提前提醒）
                    if (adv > 0 && !t.AdvanceNotified)
                    {
                        var advTime = t.Finish.AddSeconds(-adv);
                        if (advTime > now)
                            next = next == null || advTime < next ? advTime : next;
                    }

                    // 候选2：到点提醒时间（遵循 AlsoNotifyAtDue 设置）
                    if (!t.Notified)
                    {
                        if (!settings.AlsoNotifyAtDue && t.AdvanceNotified)
                        {
                            // 已提前提醒且关闭了“同时准点通知” -> 跳过到点提醒的调度
                        }
                        else if (t.Finish > now)
                        {
                            next = next == null || t.Finish < next ? t.Finish : next;
                        }
                    }
                }

                const int minMs = 1000; // 最小间隔 1 秒
                const int maxMs = 5000; // 最大间隔 5 秒（避免等待过久）
                const int guardSec = 3; // 提前量 3 秒（稍早唤醒以对冲计时抖动）

                int interval = maxMs;
                if (next.HasValue)
                {
                    var target = next.Value.AddSeconds(-guardSec);
                    if (target < now) target = now.AddMilliseconds(minMs); // 若已过期，则尽快（按最小间隔）检查
                    var deltaMs = (int)Math.Max(0, (target - now).TotalMilliseconds);
                    interval = Math.Clamp(deltaMs, minMs, maxMs);
                }

                timerTick.Interval = interval;
            }
            catch
            {
                // 忽略
            }
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

        // ---------- 管理列表 ----------
        private void ShowManager(bool isAccount)
        {
            using var dlg = new ManageListForm(isAccount ? "账号管理" : "任务管理",
                isAccount ? settings.Accounts : settings.TaskPresets);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

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

        // ---------- 删除策略开关 ----------
        private void ApplyDeletionPolicyFromSettings()
        {
            var keepSecs = settings.AutoDeleteCompletedSeconds > 0 ? settings.AutoDeleteCompletedSeconds : int.MaxValue;
            deletionPolicy = new SimpleDeletionPolicy(pendingDeleteDelaySeconds: 3, completedKeepSeconds: keepSecs);
        }

        // ---------- 窗口记忆 ----------
        private void RestoreWindowBoundsFromSettings()
        {
            try
            {
                if (settings is { WindowWidth: > 0, WindowHeight: > 0 })
                {
                    var bounds = new Rectangle(
                        settings.WindowX >= 0 ? settings.WindowX : Location.X,
                        settings.WindowY >= 0 ? settings.WindowY : Location.Y,
                        settings.WindowWidth,
                        settings.WindowHeight);

                    // 确保窗口在任何屏幕可见范围内
                    var screen = Screen.FromRectangle(bounds);
                    var wa = screen.WorkingArea;
                    var safe = Rectangle.Intersect(bounds,
                        new Rectangle(wa.X, wa.Y, Math.Max(200, wa.Width), Math.Max(200, wa.Height)));
                    if (safe is { Width: >= 400, Height: >= 300 })
                    {
                        StartPosition = FormStartPosition.Manual;
                        Bounds = bounds;
                    }
                }

                if (settings.WindowMaximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            catch
            {
                // 忽略
            }
        }

        private void UpdateWindowBoundsToSettings(bool save)
        {
            try
            {
                settings.WindowMaximized = WindowState == FormWindowState.Maximized;
                if (WindowState == FormWindowState.Normal)
                {
                    settings.WindowX = Bounds.X;
                    settings.WindowY = Bounds.Y;
                    settings.WindowWidth = Bounds.Width;
                    settings.WindowHeight = Bounds.Height;
                }

                if (save) SaveSettings();
            }
            catch
            {
                /* ignore */
            }
        }

        // ---------- 更新检查辅助 ----------
        private static Version GetCurrentVersion()
        {
            // 优先使用 AssemblyInformationalVersion；若无则用 FileVersion；再退而求其次使用 ProductVersion
            var asm = Assembly.GetExecutingAssembly();

            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                var v = info.Split('+')[0].TrimStart('v', 'V');
                if (Version.TryParse(v, out var ver)) return ver;
            }

            var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrWhiteSpace(file) && Version.TryParse(file, out var fileVer)) return fileVer;

            var prod = Application.ProductVersion;
            if (!string.IsNullOrWhiteSpace(prod) && Version.TryParse(prod, out var prodVer)) return prodVer;

            return new Version(0, 0, 0, 0);
        }

        private static Version NormalizeVersion(Version v)
        {
            // 将缺省的 Build/Revision 归一化为 0，避免 1.1.1 与 1.1.1.0 被误判为不同版本
            var build = v.Build < 0 ? 0 : v.Build;
            var rev = v.Revision < 0 ? 0 : v.Revision;
            return new Version(v.Major, v.Minor, build, rev);
        }

        private static string FormatVersionForDisplay(Version v) =>
            v.Revision == 0
                ? (v.Build == 0
                    ? $"{v.Major}.{v.Minor}"
                    : $"{v.Major}.{v.Minor}.{v.Build}")
                : $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";

        private sealed class GitHubLatestRelease
        {
            [JsonPropertyName("tag_name")] public string? TagName { get; init; }
            [JsonPropertyName("html_url")] public string? HtmlUrl { get; init; }
            [JsonPropertyName("name")] public string? Name { get; init; }
        }

        private async Task CheckForUpdatesAsync(IWin32Window owner, bool openOnNew = true)
        {
            const string apiUrl = "https://api.github.com/repos/YuanXiQWQ/Game-Upgrade-Reminder/releases/latest";
            const string releasesPage = "https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases";

            try
            {
                using var http = new HttpClient();
                // GitHub API 需要设置 User-Agent 请求头
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Game-Upgrade-Reminder/1.0 (+WinForms)");

                var resp = await http.GetAsync(apiUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    if (MessageBox.Show(owner, "无法从 GitHub 获取最新版本信息，是否打开发布页？",
                            "检查更新", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(releasesPage) { UseShellExecute = true });
                    }

                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var latest = JsonSerializer.Deserialize<GitHubLatestRelease>(json);
                if (latest == null || string.IsNullOrWhiteSpace(latest.TagName))
                {
                    MessageBox.Show(owner, "未能解析最新版本信息。", "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var current = NormalizeVersion(GetCurrentVersion());
                var tag = latest.TagName.Trim();
                var normalized = tag.TrimStart('v', 'V');

                if (!Version.TryParse(normalized, out var latestVer))
                {
                    if (MessageBox.Show(owner,
                            $"检测到最新版本标记：{tag}\n无法解析为标准版本号，是否打开发布页？",
                            "检查更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                    {
                        return;
                    }

                    var url = string.IsNullOrWhiteSpace(latest.HtmlUrl) ? releasesPage : latest.HtmlUrl;
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return;
                }

                latestVer = NormalizeVersion(latestVer);

                if (latestVer > current)
                {
                    var msg =
                        $"发现新版本：v{FormatVersionForDisplay(latestVer)}\n当前版本：v{FormatVersionForDisplay(current)}\n是否前往下载？";
                    if (!openOnNew ||
                        MessageBox.Show(owner, msg, "有可用更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information) ==
                        DialogResult.Yes)
                    {
                        var url = string.IsNullOrWhiteSpace(latest.HtmlUrl) ? releasesPage : latest.HtmlUrl;
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show(owner, $"已是最新版本（当前 v{FormatVersionForDisplay(current)}）。", "检查更新",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(owner, $"检查更新失败：{ex.Message}\n是否打开发布页？",
                        "检查更新", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(releasesPage) { UseShellExecute = true });
                }
            }
        }
    }
}