/*
 * 游戏升级提醒 - 主窗体
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 游戏升级提醒主窗口，负责UI展示和用户交互，管理升级任务的显示和操作
 * 创建日期: 2025-08-15
 * 最后修改: 2025-09-02
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game_Upgrade_Reminder.Core.Services;
using Game_Upgrade_Reminder.Infrastructure.Repositories;
using Game_Upgrade_Reminder.Infrastructure.System;
using Game_Upgrade_Reminder.Infrastructure.UI;
using Game_Upgrade_Reminder.Core.Models;
using Game_Upgrade_Reminder.Core.Abstractions;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 主窗体：负责界面布局、用户交互、任务展示与托盘交互。
    /// 主要模块：菜单、顶部账号/任务区、时间区、任务列表、托盘与通知、设置的加载/保存。
    /// </summary>
    public sealed partial class MainForm : Form
    {
        // 常量
        /// <summary>
        /// 应用程序主标题，用于窗口标题栏与通知文本等显示。
        /// </summary>
        private const string AppTitle = "游戏升级提醒";

        /// <summary>
        /// 到点或需确认任务在列表中的背景高亮色。
        /// </summary>
        private static readonly Color DueBackColor = Color.FromArgb(230, 230, 230);

        // 列宽
        /// <summary>
        /// “账号”列默认宽度（像素）。
        /// </summary>
        private const int AccountColWidth = 150;

        /// <summary>
        /// “任务”列默认宽度（像素）。
        /// </summary>
        private const int TaskColWidth = 150;

        /// <summary>
        /// “开始时间”列默认宽度（像素）。
        /// </summary>
        private const int StartTimeColWidth = 130;

        /// <summary>
        /// “持续时间”列默认宽度（像素）。
        /// </summary>
        private const int DurationColWidth = 150;

        /// <summary>
        /// “完成时间”列默认宽度（像素）。
        /// </summary>
        private const int FinishTimeColWidth = 130;

        /// <summary>
        /// “剩余时间”列默认宽度（像素）。
        /// </summary>
        private const int RemainingTimeColWidth = 150;

        /// <summary>
        /// “重复”列默认宽度（像素）。
        /// </summary>
        private const int RepeatColWidth = 240;

        /// <summary>
        /// “操作”列默认宽度（像素）。
        /// </summary>
        private const int ActionColWidth = 50;

        /// <summary>
        /// 额外留白宽度（像素），用于避免水平滚动条抖动。
        /// </summary>
        private const int ExtraSpace = 50;

        /// <summary>
        /// 黄金分割倒数常量（2/(1+sqrt(5))），用于尺寸或布局计算时的比例参考。
        /// </summary>
        private static readonly double InvPhi = 2.0 / (1.0 + Math.Sqrt(5.0));

        // 服务
        private readonly JsonTaskRepository _taskRepo = new();
        private readonly JsonSettingsStore _settingsStore = new();
        private readonly RegistryAutostartManager _autostartManager = new();
        private readonly ByFinishTimeSortStrategy _sortStrategy = new();

        private readonly ILocalizationService _localizationService =
            new JsonLocalizationService(Path.Combine(AppContext.BaseDirectory, "Resources", "Localization"));

        private IDurationFormatter? _durationFormatter;
        private readonly IDateFormatService _dateFormat;

        private SimpleDeletionPolicy _deletionPolicy =
            new(pendingDeleteDelaySeconds: 3, completedKeepSeconds: 60);

        // 状态
        private SettingsData _settings = new();
        private readonly BindingList<TaskItem> _tasks = [];
        private RepeatSpec? _currentRepeatSpec;
        private bool _followSystemStartTime;
        private bool _isUpdatingStartProgrammatically;
        private bool _userEditingStart;

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
                if (_listView.IsDisposed) return;
                if (_listView.SelectedIndices.Count > 0) return;

                // 清除每一项的 Focused 以防止系统重绘焦点框
                foreach (ListViewItem it in _listView.Items)
                {
                    if (it.Focused) it.Focused = false;
                }

                // 清除 ListView 的焦点项
                _listView.FocusedItem = null;

                // 把焦点移出到窗体（不让任何子控件获得焦点）
                if (moveFocusAway)
                {
                    ActiveControl = null;
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
                if (_listView.IsDisposed) return;
                foreach (ListViewItem it in _listView.Items)
                {
                    if (it.Focused) it.Focused = false;
                }

                _listView.FocusedItem = null;
                if (moveFocusAway)
                {
                    ActiveControl = null;
                }
            }
            catch
            {
                // 忽略
            }
        }

        // ---------- 工具/辅助 ----------
        /// <summary>
        /// 更新状态栏统计信息（总数、已到点、进行中、下一个提醒），并同步重复状态与托盘菜单显示。
        /// </summary>
        private void UpdateStatusBar()
        {
            var now = DateTime.Now;
            // 不统计“待删除”的任务
            var total = _tasks.Count(t => t is { PendingDelete: false });
            var due = _tasks.Count(t => t is { PendingDelete: false, Done: false } && t.Finish <= now);
            var pending = _tasks.Count(t => t is { PendingDelete: false, Done: false } && t.Finish > now);

            _lblTotal.Text = _localizationService.GetFormattedText("Status.Total", total);
            _lblDue.Text = _localizationService.GetFormattedText("Status.Due", due);
            _lblPending.Text = _localizationService.GetFormattedText("Status.Pending", pending);

            // 计算下一个提醒点（包含提前提醒或到点提醒）
            DateTime? next = null;
            var adv = _settings.AdvanceNotifySeconds;
            foreach (var t in _tasks)
            {
                // 与调度逻辑保持一致：跳过已完成或待删除的任务
                if (t is { PendingDelete: true } || t is { Done: true }) continue;
                var spec0 = t.Repeat;
                var isRepeat0 = spec0?.IsRepeat == true;
                // 仅当开启了“提醒后暂停”时，AwaitingAck 才阻断“下一个”时间的计算
                if (t.AwaitingAck && isRepeat0 && spec0!.PauseUntilDone) continue;

                var spec = t.Repeat;
                var isRepeat = spec?.IsRepeat == true;
                var inSkipPhase = isRepeat && ShouldSkipOccurrence(spec!, t.RepeatCursor);
                var targetFinish = inSkipPhase
                    ? ApplyRepeatOffset(t.Finish, CalcNextEffectiveOccurrence(t.Finish, spec!, t.RepeatCursor, out _), spec!)
                    : t.Finish;

                if (adv > 0 && !t.AdvanceNotified)
                {
                    var advTime = targetFinish.AddSeconds(-adv);
                    if (advTime > now) next = next is null || advTime < next ? advTime : next;
                }

                if (!t.Notified && targetFinish > now && (_settings.AlsoNotifyAtDue || !t.AdvanceNotified))
                {
                    next = next is null || targetFinish < next ? targetFinish : next;
                }
            }

            _lblNext.Text = next.HasValue
                ? _localizationService.GetFormattedText("Status.Next", $@"{(next.Value - now):hh\:mm\:ss}")
                : _localizationService.GetText("Status.NextEmpty", "下一个: -");

            // 同步托盘菜单状态
            UpdateTrayMenuStatus();
        }

        /// <summary>
        /// 将状态栏的统计文本同步到托盘菜单的只读项中，保持显示一致。
        /// </summary>
        private void UpdateTrayMenuStatus()
        {
            try
            {
                // 从状态栏文本直接同步，保持显示一致
                _miStatTotal.Text = _lblTotal.Text;
                _miStatDue.Text = _lblDue.Text;
                _miStatPending.Text = _lblPending.Text;
                _miStatNext.Text = _lblNext.Text;
            }
            catch
            {
                // 忽略
            }
        }

        /// <summary>
        /// 更新状态栏“重复”说明文本：
        /// 始终显示“默认重复设置”（来源于 <see cref="_currentRepeatSpec"/>）；
        /// 若有选中任务，追加显示“选中”任务的重复信息。
        /// </summary>
        /// <summary>
        /// 将 <see cref="RepeatSpec"/> 格式化为人类可读的中文描述（含截止、跳过与暂停说明）。
        /// </summary>
        /// <param name="spec">重复规则。</param>
        /// <returns>用于界面展示的中文描述。</returns>
        private string FormatRepeatSpec(RepeatSpec spec)
        {
            if (!spec.IsRepeat) return _localizationService.GetText("Repeat.None", "不重复");

            var parts = new List<string>();

            var modeStr = spec.Mode switch
            {
                RepeatMode.Daily => _localizationService.GetText("Repeat.Daily", "每天"),
                RepeatMode.Weekly => _localizationService.GetText("Repeat.Weekly", "每周"),
                RepeatMode.Monthly => _localizationService.GetText("Repeat.Monthly", "每月"),
                RepeatMode.Yearly => _localizationService.GetText("Repeat.Yearly", "每年"),
                RepeatMode.Custom => FormatRepeatCustom(spec.Custom),
                _ => _localizationService.GetText("Repeat.None", "不重复")
            };
            parts.Add(modeStr);

            if (spec.EndAt is not null)
            {
                parts.Add(_localizationService.GetFormattedText("Repeat.EndAt",
                    _dateFormat.FormatSmartDateTime(spec.EndAt.Value, DateTime.Now)));
            }

            if (spec is { HasSkip: true, Skip: var skipRule and not null })
            {
                parts.Add(_localizationService.GetFormattedText("Repeat.Skip", skipRule.RemindTimes, skipRule.SkipTimes));
            }

            if (spec.PauseUntilDone)
            {
                parts.Add(_localizationService.GetText("Repeat.PauseUntilDone", "提醒后暂停直到确认"));
            }

            if (spec.OffsetAfterSeconds != 0)
            {
                var dirText = spec.OffsetAfterSeconds < 0 ? "提醒后提前" : "提醒后延后";
                var abs = Math.Abs(spec.OffsetAfterSeconds);
                var h = abs / 3600;
                var m = (abs % 3600) / 60;
                var secs = abs % 60;
                var units = new List<string>();
                if (h > 0) units.Add(_localizationService.GetFormattedText("Time.Hours", h));
                if (m > 0) units.Add(_localizationService.GetFormattedText("Time.Minutes", m));
                if (secs > 0) units.Add(_localizationService.GetFormattedText("Time.Seconds", secs));
                var text = units.Count > 0 ? string.Join("", units) : "0秒";
                parts.Add($"{dirText} {text}");
            }

            return string.Join("，", parts);
        }

        /// <summary>
        /// 将自定义重复间隔格式化为中文描述。
        /// </summary>
        /// <param name="c">自定义单位（年/月/天/时/分/秒）。</param>
        /// <returns>形如“每 1年 2月 3天 ...”的描述；为空或无效时返回“自定义”。</returns>
        private string FormatRepeatCustom(RepeatCustom? c)
        {
            if (c is null or { IsEmpty: true }) return _localizationService.GetText("Repeat.Custom", "自定义");

            // 非负拷贝
            var years = Math.Max(0, c.Years);
            var months = Math.Max(0, c.Months);
            var days = Math.Max(0, c.Days);
            var hours = Math.Max(0, c.Hours);
            var minutes = Math.Max(0, c.Minutes);
            var seconds = Math.Max(0, c.Seconds);

            DurationUtils.NormalizeYmDhms(ref years, ref months, ref days, ref hours, ref minutes, ref seconds);

            var units = new List<string>();
            if (years > 0) units.Add(_localizationService.GetFormattedText("Time.Years", years));
            if (months > 0) units.Add(_localizationService.GetFormattedText("Time.Months", months));
            if (days > 0) units.Add(_localizationService.GetFormattedText("Time.Days", days));
            if (hours > 0) units.Add(_localizationService.GetFormattedText("Time.Hours", hours));
            if (minutes > 0) units.Add(_localizationService.GetFormattedText("Time.Minutes", minutes));
            if (seconds > 0) units.Add(_localizationService.GetFormattedText("Time.Seconds", seconds));
            return units.Count > 0
                ? _localizationService.GetFormattedText("Repeat.Every", string.Join(" ", units))
                : _localizationService.GetText("Repeat.Custom", "自定义");
        }

        /// <summary>
        /// 根据列表可用宽度自适应调整各列宽度：固定“时间/重复/操作”列，按比例分配“账号/任务”两列。
        /// </summary>
        private void AdjustListViewColumns()
        {
            if (_listView.Columns.Count < 9) return;

            // 固定列总宽（开始、持续、完成、剩余、操作两列）
            const int fixedSum = StartTimeColWidth + DurationColWidth + FinishTimeColWidth + RemainingTimeColWidth +
                                 RepeatColWidth +
                                 (ActionColWidth * 2);
            var available = _listView.ClientSize.Width - fixedSum - 8;
            if (available < 100) available = 100;

            // Account 与 Task 两列平均分配，并设置最小值
            const int minA = 100, minT = 120;
            var a = Math.Max(minA, available / 2);
            var t = Math.Max(minT, available - a);

            _listView.Columns[0].Width = a; // 账号
            _listView.Columns[1].Width = t; // 任务
            _listView.Columns[2].Width = StartTimeColWidth;
            _listView.Columns[3].Width = DurationColWidth;
            _listView.Columns[4].Width = FinishTimeColWidth;
            _listView.Columns[5].Width = RemainingTimeColWidth;
            _listView.Columns[6].Width = RepeatColWidth;
            _listView.Columns[7].Width = ActionColWidth;
            _listView.Columns[8].Width = ActionColWidth;
        }

        /// <summary>
        /// 处理全局快捷键（F5 刷新、Ctrl+N 新增、Ctrl+Shift+D 清理完成、Ctrl+O 打开配置、Ctrl+U 更新、Ctrl+Q 退出）。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">按键数据。</param>
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            Action? handler = e.KeyData switch
            {
                Keys.F5 => () =>
                {
                    PurgePending(force: true);
                    RefreshTable();
                },
                Keys.Control | Keys.N => () => { _btnAddSave.PerformClick(); },
                Keys.Control | Keys.Shift | Keys.D => () => { _btnDeleteDone.PerformClick(); },
                Keys.Control | Keys.O => OpenConfigFolder,
                Keys.Control | Keys.U => () => { _ = CheckForUpdatesAsync(this); },
                Keys.Control | Keys.Q => () =>
                {
                    _settings.MinimizeOnClose = false;
                    Close();
                },
                _ => null
            };

            if (handler is null) return;

            e.Handled = true;
            handler();
        }

        /// <summary>
        /// 打开应用程序配置文件所在目录（AppContext.BaseDirectory）。
        /// </summary>
        private void OpenConfigFolder()
        {
            try
            {
                var folder = AppContext.BaseDirectory;
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localizationService.GetFormattedText("Error.FailedToOpenConfigFolder", ex.Message),
                    _localizationService.GetText("Error.Title", "错误"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private SortMode _sortMode = SortMode.DefaultByFinish;
        private int _customSortColumn = 4;
        private bool _customSortAsc = true;

        private readonly DateTimePicker _dtpStart = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            ShowUpDown = true,
            Width = 160
        };

        // 字体
        private Font? _strikeFont;

        // Controls
        private readonly ComboBox _cbAccount = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        private readonly Button _btnAccountMgr = new();

        private readonly ComboBox _cbTask = new()
        {
            DropDownStyle = ComboBoxStyle.DropDown
        };

        private readonly Button _btnTaskMgr = new();

        private readonly Button _btnDeleteDone = new();

        private readonly Button _btnRefresh = new();

        private readonly Button _btnNow = new();

        private readonly NumericUpDown _numDays = new()
        {
            Minimum = 0,
            Maximum = 3650,
            Width = 40
        };

        private readonly NumericUpDown _numHours = new()
        {
            Minimum = 0,
            Maximum = 1000,
            Width = 40
        };

        private readonly NumericUpDown _numMinutes = new()
        {
            Minimum = 0,
            Maximum = 59,
            Width = 40
        };

        private readonly TextBox _tbFinish = new()
        {
            ReadOnly = true
        };

        private readonly Button _btnAddSave = new();

        private readonly Button _btnRepeat = new();

        private readonly Button _btnClear = new();

        // 可本地化标签字段（用于语言切换时动态更新）
        private Label _lbAccount = new();
        private Label _lbTask = new();
        private Label _lbStartTime = new();
        private Label _lbDays = new();
        private Label _lbHours = new();
        private Label _lbMinutes = new();
        private Label _lbFinishTime = new();

        // 双缓冲 ListView
        /// <summary>
        /// 双缓冲的 ListView，开启 DoubleBuffered 以减少重绘闪烁。
        /// </summary>
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

        private readonly ListView _listView = new DoubleBufferedListView();

        // 菜单与托盘
        private readonly MenuStrip _menu = new();
        private readonly ToolStripMenuItem _miSettings = new();
        private readonly ToolStripMenuItem _miFont = new();
        private readonly ToolStripMenuItem _miAutoStart = new();
        private readonly ToolStripMenuItem _miLanguage = new();

        // 悬浮显示的三个菜单项
        private readonly ToolStripMenuItem _miOpenConfig = new();

        private readonly ToolStripMenuItem _miResetWindow = new();

        private readonly ToolStripMenuItem _miAutoDelete = new();
        private readonly ToolStripMenuItem _miDelOff = new();
        private readonly ToolStripMenuItem _miDel30S = new();
        private readonly ToolStripMenuItem _miDel1M = new();
        private readonly ToolStripMenuItem _miDel3M = new();
        private readonly ToolStripMenuItem _miDel30M = new();
        private readonly ToolStripMenuItem _miDel1H = new();
        private readonly ToolStripMenuItem _miDelCustom = new();
        private readonly ToolStripMenuItem _miAdvanceNotify = new();
        private readonly ToolStripMenuItem _miAdvOff = new();
        private readonly ToolStripMenuItem _miAdvAlsoDue = new();
        private readonly ToolStripMenuItem _miAdv30S = new();
        private readonly ToolStripMenuItem _miAdv1M = new();
        private readonly ToolStripMenuItem _miAdv3M = new();
        private readonly ToolStripMenuItem _miAdv30M = new();
        private readonly ToolStripMenuItem _miAdv1H = new();
        private readonly ToolStripMenuItem _miAdvCustom = new();
        private readonly ToolStripMenuItem _miCloseBehavior = new();
        private readonly ToolStripMenuItem _miCloseExit = new();
        private readonly ToolStripMenuItem _miCloseMinimize = new();
        private readonly ToolStripMenuItem _miAboutTop = new();

        private readonly NotifyIcon _tray = new();
        private readonly ContextMenuStrip _trayMenu = new();
        private readonly TrayNotifier _notifier;

        // 托盘状态项
        private readonly ToolStripMenuItem _miStatHeader = new() { Enabled = false };
        private readonly ToolStripMenuItem _miStatTotal = new() { Enabled = false };
        private readonly ToolStripMenuItem _miStatDue = new() { Enabled = false };
        private readonly ToolStripMenuItem _miStatPending = new() { Enabled = false };
        private readonly ToolStripMenuItem _miStatNext = new() { Enabled = false };

        // 托盘操作项（可复用，便于动态更新文本）
        private readonly ToolStripMenuItem _miTrayOpen = new();
        private readonly ToolStripMenuItem _miTrayRefresh = new();
        private readonly ToolStripMenuItem _miTrayCheckUpdate = new();
        private readonly ToolStripMenuItem _miTrayOpenConfig = new();
        private readonly ToolStripMenuItem _miTrayExit = new();

        // 底部状态栏
        private readonly StatusStrip _status = new();
        private readonly ToolStripStatusLabel _lblTotal = new();
        private readonly ToolStripStatusLabel _lblDue = new();
        private readonly ToolStripStatusLabel _lblPending = new();
        private readonly ToolStripStatusLabel _lblNext = new();
        private readonly ToolStripStatusLabel _lblCellContent = new();

        // 计时器
        private readonly System.Windows.Forms.Timer _timerTick = new() { Interval = 1_000 };
        private readonly System.Windows.Forms.Timer _timerUi = new() { Interval = 1000 };

        private readonly System.Windows.Forms.Timer _timerPurge = new() { Interval = 500 };

        // 菜单悬浮自动关闭控制
        private readonly System.Windows.Forms.Timer _hoverMenuTimer = new() { Interval = 200 };
        private ToolStripMenuItem? _hoverPendingClose;

        // ---------- 原生 Header 箭头（与系统主题同步） ----------
        /// <summary>
        /// 对应 Win32 HDITEM 结构，用于读取/设置列头格式（含排序箭头）。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct HeaderItem
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

        /// <summary>
        /// ListView 消息基值（LVM_FIRST）。
        /// </summary>
        private const int LvmFirst = 0x1000;

        /// <summary>
        /// 获取 ListView 头部窗口句柄的消息。
        /// </summary>
        private const int LvmGetHeader = LvmFirst + 31;

        /// <summary>
        /// Header 控件消息基值（HDM_FIRST）。
        /// </summary>
        private const int HdmFirst = 0x1200;

        /// <summary>
        /// 获取列头项（HDITEM）的消息。
        /// </summary>
        private const int HdmGetItem = HdmFirst + 11;

        /// <summary>
        /// 设置列头项（HDITEM）的消息。
        /// </summary>
        private const int HdmSetItem = HdmFirst + 12;

        /// <summary>
        /// HDITEM 中 fmt 字段掩码（HDI_FORMAT）。
        /// </summary>
        private const int HdiFormat = 0x0004;

        /// <summary>
        /// 升序箭头标志。
        /// </summary>
        private const int HdfSortUp = 0x0400;

        /// <summary>
        /// 降序箭头标志。
        /// </summary>
        private const int HdfSortDown = 0x0200;

        /// <summary>
        /// 发送窗口消息（无托管结构体版本）。
        /// </summary>
        /// <param name="hWnd">目标窗口句柄。</param>
        /// <param name="msg">消息编号。</param>
        /// <param name="wParam">消息参数1。</param>
        /// <param name="lParam">消息参数2。</param>
        /// <returns>消息返回值。</returns>
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 发送窗口消息（HDITEM 结构体版本）。
        /// </summary>
        /// <param name="hWnd">Header 句柄。</param>
        /// <param name="msg">消息编号。</param>
        /// <param name="wParam">列索引（IntPtr）。</param>
        /// <param name="lParam">HDITEM 结构体引用。</param>
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial void SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref HeaderItem lParam);

        /// <summary>
        /// 根据当前排序模式更新原生列头的升/降序箭头显示。
        /// 自定义排序仅对前 7 列生效，默认排序固定为“剩余时间”升序。
        /// </summary>
        private void UpdateListViewSortArrow()
        {
            if (_listView.IsDisposed || _listView.Handle == IntPtr.Zero || _listView.Columns.Count == 0) return;
            var header = SendMessage(_listView.Handle, LvmGetHeader, IntPtr.Zero, IntPtr.Zero);
            if (header == IntPtr.Zero) return;

            // 计算当前应显示箭头的列与方向：
            // - 自定义排序：使用 _customSortColumn/_customSortAsc
            // - 默认排序：使用 剩余时间列(索引5) 升序
            var sortedColumn = -1;
            var sortedAsc = true;
            if (_sortMode == SortMode.Custom)
            {
                // 仅允许 0..6 这些“可排序列”显示自定义排序箭头；7/8（完成/删除）不显示
                if (_customSortColumn is >= 0 and <= 6)
                {
                    sortedColumn = _customSortColumn;
                    sortedAsc = _customSortAsc;
                }
                else
                {
                    sortedColumn = -1;
                }
            }
            else // SortMode.DefaultByFinish
            {
                if (_listView.Columns.Count > 5)
                {
                    sortedColumn = 5; // 剩余时间列
                    sortedAsc = true; // 升序
                }
            }

            for (var i = 0; i < _listView.Columns.Count; i++)
            {
                var item = new HeaderItem { mask = HdiFormat };
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

        /// <summary>
        /// 初始化主窗口：构建菜单与界面、加载设置与任务，
        /// 同步自启动状态，启动计时器并初始化托盘与初始调度。
        /// </summary>
        public MainForm()
        {
            Text = AppTitle;
            // 根据语言自动应用 RTL，并在语言切换时动态更新
            RtlHelper.ApplyAndBind(_localizationService, this);
            _dateFormat = new LocalizedDateFormatService(_localizationService);
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

            // 默认窗口尺寸、位置
            const int totalWidth = AccountColWidth + TaskColWidth + StartTimeColWidth + DurationColWidth +
                                   FinishTimeColWidth + RemainingTimeColWidth + RepeatColWidth + (ActionColWidth * 2) +
                                   ExtraSpace;
            var defaultHeight = (int)Math.Round(totalWidth * InvPhi);
            ClientSize = new Size(totalWidth, defaultHeight);
            StartPosition = FormStartPosition.CenterScreen;

            _notifier = new TrayNotifier(_tray);

            BuildMenu();
            BuildUi();
            WireEvents();

            // 语言切换时更新日期格式与列表显示
            _localizationService.LanguageChanged += (_, _) =>
            {
                try
                {
                    _dtpStart.CustomFormat = _dateFormat.GetDatePickerDateTimeFormat();
                    RefreshTable();
                }
                catch
                {
                    // 忽略
                }
            };

            // 加载设置与任务
            LoadSettings();
            ApplyDeletionPolicyFromSettings();
            ApplySettingsToUi();
            RestoreWindowBoundsFromSettings();

            // 初始化本地化
            InitializeLocalization();
            LoadTasks();
            if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
            RefreshTable();

            // 同步自启动状态
            var actuallyOn = _autostartManager.IsEnabled();
            if (actuallyOn != _settings.StartupOnBoot)
            {
                _settings.StartupOnBoot = actuallyOn;
                SaveSettings();
                UpdateMenuChecks();
            }

            // 启动计时器
            _timerTick.Start();
            _timerUi.Start();
            _timerPurge.Start();

            // 初始开始时间跟随系统时间
            _isUpdatingStartProgrammatically = true;
            _dtpStart.Value = DateTime.Now;
            _isUpdatingStartProgrammatically = false;
            _followSystemStartTime = true;
            RecalcFinishFromFields();

            // 托盘初始化
            InitTray();

            // 初始调度（根据最近的提醒点设置计时器间隔）
            RescheduleNextTick();

            // 首次显示时：同步列头箭头，并把焦点移出列表，去掉默认的虚线焦点框
            Shown += (_, _) =>
            {
                UpdateListViewSortArrow();
                ActiveControl = null;
                ClearListViewFocusIfNoSelection();
            };
        }

        // ---------- 菜单 / 界面 ----------
        /// <summary>
        /// 构建主菜单与设置项，包括字体、开机自启、自动删除、提前通知与关闭按钮行为等。
        /// </summary>
        private void BuildMenu()
        {
            _miCloseBehavior.DropDownItems.AddRange([_miCloseExit, _miCloseMinimize]);

            // 已完成任务自动删除 预设
            _miAutoDelete.DropDownItems.AddRange([
                _miDelOff,
                new ToolStripSeparator(),
                _miDel30S,
                _miDel1M,
                _miDel3M,
                _miDel30M,
                _miDel1H,
                new ToolStripSeparator(),
                _miDelCustom
            ]);

            // 提前通知预设
            _miAdvanceNotify.DropDownItems.AddRange([
                _miAdvOff,
                _miAdvAlsoDue,
                new ToolStripSeparator(),
                _miAdv30S,
                _miAdv1M,
                _miAdv3M,
                _miAdv30M,
                _miAdv1H,
                new ToolStripSeparator(),
                _miAdvCustom
            ]);

            // 构建语言菜单
            BuildLanguageMenu();

            _miSettings.DropDownItems.Add(_miFont);
            _miSettings.DropDownItems.Add(_miLanguage);
            _miSettings.DropDownItems.Add(new ToolStripSeparator());
            _miSettings.DropDownItems.Add(_miAutoStart);
            _miSettings.DropDownItems.Add(_miOpenConfig);
            _miSettings.DropDownItems.Add(_miResetWindow);
            _miSettings.DropDownItems.Add(_miAutoDelete);
            _miSettings.DropDownItems.Add(_miAdvanceNotify);
            _miSettings.DropDownItems.Add(new ToolStripSeparator());
            _miSettings.DropDownItems.Add(_miCloseBehavior);

            _menu.Items.Add(_miSettings);
            _miAboutTop.Alignment = ToolStripItemAlignment.Left;
            _menu.Items.Add(_miAboutTop);

            MainMenuStrip = _menu;
            Controls.Add(_menu);
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
                _hoverPendingClose = null;
                _hoverMenuTimer.Stop();
            };

            // 鼠标离开：启动一个短延时检测，若鼠标不在此项或其下拉上则关闭
            mi.MouseLeave += (_, _) =>
            {
                _hoverPendingClose = mi;
                _hoverMenuTimer.Stop();
                _hoverMenuTimer.Start();
            };

            // 移动到下拉时，取消待关闭；离开下拉后开始检测
            mi.DropDown.MouseEnter += (_, _) =>
            {
                if (_hoverPendingClose == mi)
                {
                    _hoverPendingClose = null;
                    _hoverMenuTimer.Stop();
                }
            };
            mi.DropDown.MouseLeave += (_, _) =>
            {
                _hoverPendingClose = mi;
                _hoverMenuTimer.Stop();
                _hoverMenuTimer.Start();
            };
        }

        /// <summary>
        /// 关闭除 current 之外的其他“悬浮显示”菜单，以避免多个下拉同时可见。
        /// </summary>
        private void CloseOtherHoverMenus(ToolStripMenuItem current)
        {
            foreach (var other in new[] { _miAutoDelete, _miAdvanceNotify, _miCloseBehavior })
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

            _lbAccount = MakeAutoLabel(_localizationService.GetText("Control.Account.Name", "账号"));
            line1.Controls.Add(_lbAccount, 0, 0);

            _cbAccount.Width = 220;
            _cbAccount.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cbAccount.Margin = new Padding(0, 2, 6, 2);
            line1.Controls.Add(_cbAccount, 1, 0);
            _btnAccountMgr.AutoSize = true;
            _btnAccountMgr.Anchor = AnchorStyles.Left;
            _btnAccountMgr.Margin = new Padding(0, 2, 0, 2);
            line1.Controls.Add(_btnAccountMgr, 2, 0);

            _lbTask = MakeAutoLabel(_localizationService.GetText("Control.Task.Name", "任务"));
            line1.Controls.Add(_lbTask, 4, 0);

            _cbTask.Width = 240;
            _cbTask.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _cbTask.Margin = new Padding(0, 2, 6, 2);
            line1.Controls.Add(_cbTask, 5, 0);

            _btnTaskMgr.AutoSize = true;
            _btnTaskMgr.Anchor = AnchorStyles.Left;
            _btnTaskMgr.Margin = new Padding(0, 2, 0, 2);
            line1.Controls.Add(_btnTaskMgr, 6, 0);
            StyleSmallButton(_btnDeleteDone);
            StyleSmallButton(_btnRefresh);

            line1.Controls.Add(_btnDeleteDone, 8, 0);
            line1.Controls.Add(_btnRefresh, 9, 0);

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
                ColumnCount = 15,
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
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lbStartTime = MakeAutoLabel(_localizationService.GetText("UI.StartTime", "开始时间"));
            line2.Controls.Add(_lbStartTime, 0, 0);

            _dtpStart.Format = DateTimePickerFormat.Custom;
            _dtpStart.CustomFormat = _dateFormat.GetDatePickerDateTimeFormat();
            _dtpStart.ShowUpDown = false;
            _dtpStart.Margin = new Padding(0, 2, 6, 2);
            _dtpStart.Anchor = AnchorStyles.Left;
            line2.Controls.Add(_dtpStart, 1, 0);

            _btnNow.AutoSize = true;
            _btnNow.Anchor = AnchorStyles.Left;
            _btnNow.Margin = new Padding(0, 0, 0, 0);
            line2.Controls.Add(_btnNow, 2, 0);

            _numDays.Width = 50;
            _numDays.Margin = new Padding(0, 2, 4, 2);
            _numDays.Anchor = AnchorStyles.Left;
            _numHours.Width = 50;
            _numHours.Margin = new Padding(0, 2, 4, 2);
            _numHours.Anchor = AnchorStyles.Left;
            _numMinutes.Width = 50;
            _numMinutes.Margin = new Padding(0, 2, 4, 2);
            _numMinutes.Anchor = AnchorStyles.Left;

            line2.Controls.Add(_numDays, 4, 0);
            _lbDays = MakeAutoLabel(_localizationService.GetText("UI.Days", "天"));
            line2.Controls.Add(_lbDays, 5, 0);
            line2.Controls.Add(_numHours, 6, 0);
            _lbHours = MakeAutoLabel(_localizationService.GetText("UI.Hours", "小时"));
            line2.Controls.Add(_lbHours, 7, 0);
            line2.Controls.Add(_numMinutes, 8, 0);
            _lbMinutes = MakeAutoLabel(_localizationService.GetText("UI.Minutes", "分钟"));
            line2.Controls.Add(_lbMinutes, 9, 0);

            _lbFinishTime = MakeAutoLabel(_localizationService.GetText("UI.FinishTime", "完成时间"));
            line2.Controls.Add(_lbFinishTime, 10, 0);
            _tbFinish.ReadOnly = true;
            _tbFinish.Margin = new Padding(0, 2, 6, 2);
            _tbFinish.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            line2.Controls.Add(_tbFinish, 11, 0);

            // 将“添加/重复”放入同一单元格中的水平面板，避免列样式造成的额外空隙
            var actionsPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            StyleSmallButton(_btnRepeat, new Padding(0, 2, 0, 2));
            actionsPanel.Controls.Add(_btnRepeat, 0, 0);

            StyleSmallButton(_btnClear, new Padding(6, 2, 0, 2));
            actionsPanel.Controls.Add(_btnClear, 1, 0);

            StyleSmallButton(_btnAddSave, new Padding(6, 2, 0, 2));
            actionsPanel.Controls.Add(_btnAddSave, 2, 0);

            line2.Controls.Add(actionsPanel, 12, 0);

            gbTime.Controls.Add(line2);
            root.Controls.Add(gbTime, 0, 1);

            // 第 3 行（任务列表）
            _listView.Dock = DockStyle.Fill;
            _listView.FullRowSelect = true;
            _listView.GridLines = true;
            _listView.HideSelection = false;
            _listView.MultiSelect = false;
            _listView.View = View.Details;
            root.Controls.Add(_listView, 0, 2);

            InitListViewColumns();
            AdjustListViewColumns();
            ApplyListViewRtlLayout();

            // 底部状态栏
            _status.SizingGrip = true;
            _status.Items.Add(_lblTotal);
            _status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            _status.Items.Add(_lblDue);
            _status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            _status.Items.Add(_lblPending);
            _status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            _status.Items.Add(_lblNext);
            _status.Items.Add(new ToolStripStatusLabel("|") { ForeColor = SystemColors.ControlDark });
            _status.Items.Add(_lblCellContent);
            root.Controls.Add(_status, 0, 3);
        }

        /// <summary>
        /// 初始化任务列表列头与列宽，确保与常量宽度保持一致。
        /// </summary>
        private void InitListViewColumns()
        {
            _listView.Columns.Clear();
            _listView.Columns.AddRange(
            [
                new ColumnHeader
                    { Text = _localizationService.GetText("ListView.Column.Account", "账号"), Width = AccountColWidth },
                new ColumnHeader
                    { Text = _localizationService.GetText("ListView.Column.Task", "任务"), Width = TaskColWidth },
                new ColumnHeader
                {
                    Text = _localizationService.GetText("ListView.Column.StartTime", "开始时间"), Width = StartTimeColWidth
                },
                new ColumnHeader
                {
                    Text = _localizationService.GetText("ListView.Column.Duration", "持续时间"), Width = DurationColWidth
                },
                new ColumnHeader
                {
                    Text = _localizationService.GetText("ListView.Column.FinishTime", "完成时间"),
                    Width = FinishTimeColWidth
                },
                new ColumnHeader
                {
                    Text = _localizationService.GetText("ListView.Column.RemainingTime", "剩余时间"),
                    Width = RemainingTimeColWidth
                },
                new ColumnHeader
                    { Text = _localizationService.GetText("ListView.Column.Repeat", "重复"), Width = RepeatColWidth },
                new ColumnHeader
                    { Text = _localizationService.GetText("ListView.Column.Complete", "完成"), Width = ActionColWidth },
                new ColumnHeader
                    { Text = _localizationService.GetText("ListView.Column.Delete", "删除"), Width = ActionColWidth }
            ]);
        }

        /// <summary>
        /// 根据当前语言应用 ListView 的 RTL 布局与列文本对齐。
        /// </summary>
        private void ApplyListViewRtlLayout()
        {
            try
            {
                var rtl = RtlHelper.IsRtlLanguage(_localizationService.CurrentLanguage);
                _listView.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
                _listView.RightToLeftLayout = rtl;

                if (_listView.Columns.Count < 9) return;

                // 文本列（账号、任务、重复）在 RTL 下右对齐，其他列居中更稳妥
                var textAlign = rtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                _listView.Columns[0].TextAlign = textAlign; // 账号
                _listView.Columns[1].TextAlign = textAlign; // 任务
                _listView.Columns[6].TextAlign = textAlign; // 重复

                // 时间/数值列统一居中，兼顾双向文本
                _listView.Columns[2].TextAlign = HorizontalAlignment.Center; // 开始时间
                _listView.Columns[3].TextAlign = HorizontalAlignment.Center; // 持续时间
                _listView.Columns[4].TextAlign = HorizontalAlignment.Center; // 完成时间
                _listView.Columns[5].TextAlign = HorizontalAlignment.Center; // 剩余时间

                // 操作列居中
                _listView.Columns[7].TextAlign = HorizontalAlignment.Center; // 完成
                _listView.Columns[8].TextAlign = HorizontalAlignment.Center; // 删除
            }
            catch
            {
                // 忽略个别环境下的设置失败
            }
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
        /// 根据当前语言应用托盘菜单的 RTL 布局。
        /// </summary>
        private void ApplyTrayRtlLayout()
        {
            try
            {
                var rtl = RtlHelper.IsRtlLanguage(_localizationService.CurrentLanguage);
                _trayMenu.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
            }
            catch
            {
                /* 忽略 */
            }
        }

        /// <summary>
        /// 统一绑定菜单、输入控件、列表、计时器等事件处理。
        /// 仅连接事件与状态更新，不包含业务数据加载。
        /// </summary>
        private void WireEvents()
        {
            // 菜单事件
            _miFont.Click += (_, _) => DoChooseFont();
            _miAutoStart.Click += (_, _) => ToggleAutostart();
            _miOpenConfig.Click += (_, _) => OpenConfigFolder();
            _miResetWindow.Click += (_, _) => ResetWindowToDefault();
            // 自动删除下拉
            _miAutoDelete.DropDownOpening += (_, _) => UpdateAutoDeleteMenuChecks();
            _miDelOff.Click += (_, _) => SetAutoDeleteSecondsAndSave(0);
            _miDel30S.Click += (_, _) => SetAutoDeleteSecondsAndSave(30);
            _miDel1M.Click += (_, _) => SetAutoDeleteSecondsAndSave(60);
            _miDel3M.Click += (_, _) => SetAutoDeleteSecondsAndSave(180);
            _miDel30M.Click += (_, _) => SetAutoDeleteSecondsAndSave(1800);
            _miDel1H.Click += (_, _) => SetAutoDeleteSecondsAndSave(3600);
            _miDelCustom.Click += (_, _) =>
            {
                using var dlg = new AdvanceTimeDialog(_localizationService, _settings.AutoDeleteCompletedSeconds);
                if (dlg.ShowDialog(this) is DialogResult.OK)
                {
                    SetAutoDeleteSecondsAndSave(dlg.TotalSeconds);
                }
            };
            _miAdvanceNotify.DropDownOpening += (_, _) => UpdateAdvanceMenuChecks();
            _miAdvOff.Click += (_, _) => SetAdvanceSecondsAndSave(0);
            _miAdv30S.Click += (_, _) => SetAdvanceSecondsAndSave(30);
            _miAdv1M.Click += (_, _) => SetAdvanceSecondsAndSave(60);
            _miAdv3M.Click += (_, _) => SetAdvanceSecondsAndSave(180);
            _miAdv30M.Click += (_, _) => SetAdvanceSecondsAndSave(1800);
            _miAdv1H.Click += (_, _) => SetAdvanceSecondsAndSave(3600);
            _miAdvAlsoDue.Click += (_, _) =>
            {
                _settings.AlsoNotifyAtDue = !_settings.AlsoNotifyAtDue;
                SaveSettings();
                UpdateAdvanceMenuChecks();
                RescheduleNextTick();
            };
            _miAdvCustom.Click += (_, _) =>
            {
                using var dlg = new AdvanceTimeDialog(_localizationService, _settings.AdvanceNotifySeconds);
                if (dlg.ShowDialog(this) is DialogResult.OK)
                {
                    SetAdvanceSecondsAndSave(dlg.TotalSeconds);
                }
            };
            // 悬浮显示/自动关闭：统一设置
            SetupHoverDropdown(_miAutoDelete);
            SetupHoverDropdown(_miAdvanceNotify);
            SetupHoverDropdown(_miCloseBehavior);
            _hoverMenuTimer.Tick += (_, _) =>
            {
                if (_hoverPendingClose is null)
                {
                    _hoverMenuTimer.Stop();
                    return;
                }

                var mi = _hoverPendingClose;
                var pt = Control.MousePosition;
                // 若鼠标不在菜单项或其下拉上则关闭
                var overItem = IsMouseOverMenuItem(mi, pt);
                var overDrop = mi.DropDown.Visible && mi.DropDown.Bounds.Contains(pt);
                if (overItem || overDrop) return;
                mi.HideDropDown();
                _hoverPendingClose = null;
                _hoverMenuTimer.Stop();
            };
            _miCloseMinimize.Click += (_, _) =>
            {
                _settings.MinimizeOnClose = true;
                SaveSettings();
                UpdateMenuChecks();
            };
            _miAboutTop.Click += (_, _) => ShowAboutDialog();

            // 开始时间编辑检测
            _dtpStart.MouseDown += (_, _) => _userEditingStart = true;
            _dtpStart.KeyDown += (_, _) => _userEditingStart = true;
            _dtpStart.CloseUp += (_, _) => _userEditingStart = true;

            _dtpStart.ValueChanged += (_, _) =>
            {
                if (!_isUpdatingStartProgrammatically && _userEditingStart) _followSystemStartTime = false;
                _userEditingStart = false;
                RecalcFinishFromFields();
            };
            _numDays.ValueChanged += (_, _) => RecalcFinishFromFields();
            _numHours.ValueChanged += (_, _) => RecalcFinishFromFields();
            _numMinutes.ValueChanged += (_, _) => RecalcFinishFromFields();

            _btnNow.Click += (_, _) =>
            {
                _followSystemStartTime = true;
                _isUpdatingStartProgrammatically = true;
                _dtpStart.Value = DateTime.Now;
                _isUpdatingStartProgrammatically = false;
                RecalcFinishFromFields();
            };

            _btnAddSave.Click += (_, _) =>
            {
                _followSystemStartTime = true;
                _isUpdatingStartProgrammatically = true;
                _dtpStart.Value = DateTime.Now;
                _isUpdatingStartProgrammatically = false;

                RecalcFinishFromFields();
                AddOrSaveTask();
            };

            _btnRepeat.Click += (_, _) =>
            {
                var selectedTask = _listView.SelectedItems.Count > 0
                    ? _listView.SelectedItems[0].Tag as TaskItem
                    : null;
                using var dlg = new RepeatSettingsForm(_localizationService, _dateFormat);
                dlg.CurrentSpec = selectedTask?.Repeat ??
                                  _currentRepeatSpec ?? new RepeatSpec { Mode = RepeatMode.None };
                dlg.RepeatSpecChanged += (_, spec) =>
                {
                    if (selectedTask is not null)
                    {
                        selectedTask.Repeat = spec;
                        selectedTask.RepeatCount = 0; // 编辑重复设置后计数清零
                        SaveTasks();
                        RefreshTable();
                        UpdateStatusBar();
                    }
                    else
                    {
                        _currentRepeatSpec = spec; // 新增任务默认值
                    }
                };

                if (dlg.ShowDialog(this) is not DialogResult.OK) return;
                var spec = dlg.CurrentSpec;
                _currentRepeatSpec = spec; // 作为默认应用到后续新增任务

                if (selectedTask is null) return;
                selectedTask.Repeat = spec;
                selectedTask.RepeatCount = 0; // 编辑重复设置后计数清零
                SaveTasks();
                RefreshTable();
                UpdateStatusBar();
            };

            _btnClear.Click += (_, _) =>
            {
                // 清空持续时长输入（天/小时/分钟）——仅作用于输入控件
                _numDays.Value = 0;
                _numHours.Value = 0;
                _numMinutes.Value = 0;
                RecalcFinishFromFields();

                // 同时将“默认重复设置”恢复为“不重复”，仅影响后续新增任务，不修改现有任务
                _currentRepeatSpec = new RepeatSpec { Mode = RepeatMode.None };
                UpdateStatusBar(); // 刷新状态栏“默认重复设置”显示
            };

            _btnDeleteDone.Click += (_, _) =>
            {
                DeleteAllDone();
                SaveTasks();
                RefreshTable();
            };
            _btnRefresh.Click += (_, _) =>
            {
                PurgePending(force: true);
                RefreshTable();
            };

            _btnAccountMgr.Click += (_, _) => ShowManager(isAccount: true);
            _btnTaskMgr.Click += (_, _) => ShowManager(isAccount: false);

            // 列表行为
            _listView.ItemActivate += (_, _) => HandleListClick();
            _listView.MouseUp += (_, me) =>
            {
                if (me.Button == MouseButtons.Left) HandleListClick();
            };
            _listView.MouseClick += HandleListViewCellClick;
            _listView.Resize += (_, _) => AdjustListViewColumns();
            _listView.GotFocus += (_, _) =>
            {
                // 当列表获得焦点但没有选中项时，清除焦点项避免虚线焦点框
                ClearListViewFocusIfNoSelection();
            };
            _listView.SelectedIndexChanged += (_, _) => { };

            // 列头点击排序（文本为主，部分列有特殊处理）
            _listView.ColumnClick += (_, e) =>
            {
                var column = e.Column;
                // 完成(7)/删除(8) 列：不参与排序，也不显示箭头
                if (column >= 7) return;

                _sortMode = SortMode.Custom;
                var sortAscending = column != _customSortColumn || !_customSortAsc;

                _listView.ListViewItemSorter = new ListViewItemComparer(column, sortAscending);
                _listView.Sort();

                _customSortColumn = column;
                _customSortAsc = sortAscending;

                var newOrder = new List<TaskItem>();
                foreach (ListViewItem row in _listView.Items)
                    if (row.Tag is TaskItem tsk)
                        newOrder.Add(tsk);

                _tasks.Clear();
                foreach (var t in newOrder) _tasks.Add(t);

                SaveTasks();
                UpdateListViewSortArrow();

                // 异步移除虚线焦点框并移走焦点，确保在排序后的消息循环稳定后处理
                BeginInvoke(() =>
                {
                    if (_listView.SelectedIndices.Count != 0) return;
                    ClearListViewFocusRegardlessOfSelection();
                    ActiveControl = null;
                });
            };

            // 计时器
            _timerTick.Tick += (_, _) => CheckDueAndNotify();
            _timerUi.Tick += (_, _) =>
            {
                UpdateRemainingCells();
                RepaintStyles();

                if (!_followSystemStartTime || DateTime.Now.Second != 0) return;
                _isUpdatingStartProgrammatically = true;
                _dtpStart.Value = DateTime.Now;
                _isUpdatingStartProgrammatically = false;
            };
            _timerPurge.Tick += (_, _) => PurgePending(force: false);

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

        // ---------- ListView 单元格点击处理 ----------
        /// <summary>
        /// 处理ListView单元格点击事件，在状态栏显示点击单元格的完整内容。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">鼠标点击事件参数。</param>
        private void HandleListViewCellClick(object? sender, MouseEventArgs e)
        {
            if (_listView.IsDisposed || e.Button != MouseButtons.Left) return;

            try
            {
                // 获取点击位置的项和子项信息
                var hitTest = _listView.HitTest(e.Location);
                if (hitTest.Item is null)
                {
                    _lblCellContent.Text = "";
                    return;
                }

                var item = hitTest.Item;
                var subItemIndex = hitTest.SubItem is not null ? item.SubItems.IndexOf(hitTest.SubItem) : 0;

                // 确保子项索引在有效范围内
                if (subItemIndex < 0 || subItemIndex >= item.SubItems.Count)
                {
                    _lblCellContent.Text = "";
                    return;
                }

                // 获取单元格内容
                var cellContent = item.SubItems[subItemIndex].Text;

                // 在状态栏显示内容
                _lblCellContent.Text = string.IsNullOrWhiteSpace(cellContent) ? "" : cellContent;
            }
            catch
            {
                _lblCellContent.Text = "";
            }
        }

        // ---------- 关于对话框（可点击链接 + 检查更新按钮） ----------
        /// <summary>
        /// 显示“关于”对话框：包含版本信息、版权与许可证、项目链接，并提供“检查更新”。
        /// 使用主窗体作为更新检查的宿主，避免捕获临时对话框实例。
        /// </summary>
        private void ShowAboutDialog()
        {
            var appDisplayName = _localizationService.GetText("About.AppName", "游戏升级提醒");
            const string gitHubUrl = "https://github.com/YuanXiQWQ/Game-Upgrade-Reminder";
            const string licenseUrl = "https://www.gnu.org/licenses/agpl-3.0.html";

            var ver = GetCurrentVersion();
            var versionText = string.Format(_localizationService.GetText("About.VersionText", "版本 v{0}"),
                $"{ver.Major}.{ver.Minor}.{ver.Build}" + (ver.Revision > 0 ? $".{ver.Revision}" : ""));
            var rtl = RtlHelper.IsRtlLanguage(_localizationService.CurrentLanguage);

            using var dlg = new Form();
            dlg.Text = _localizationService.GetText("About.Title", "关于");
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
            header.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;

            var pic = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(64, 64),
                Margin = new Padding(0, 0, 16, 0),
                Image = (Icon ?? SystemIcons.Information).ToBitmap()
            };
            var colPic = rtl ? 1 : 0;
            header.Controls.Add(pic, colPic, 0);

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
                Text = _localizationService.GetText("About.Description", "游戏升级计时与提醒工具（说不定还能拿来干点其它的:>）"),
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

            var colText = rtl ? 0 : 1;
            header.Controls.Add(headerRight, colText, 0);
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


            card.Controls.Add(CreateMetaLabel(_localizationService.GetText("About.Version", "版本")));
            card.Controls.Add(CreateMetaValue(versionText));

            card.Controls.Add(CreateMetaLabel(_localizationService.GetText("About.Copyright", "版权")));
            card.Controls.Add(CreateMetaValue(" 2025 YuanXiQWQ  •  AGPL-3.0"));

            card.Controls.Add(CreateMetaLabel(_localizationService.GetText("About.License", "许可证")));
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
            var btnGitHub = new Button
                { AutoSize = true, Text = _localizationService.GetText("About.BtnGitHub", "打开 GitHub") };
            btnGitHub.Click += (_, _) => Process.Start(new ProcessStartInfo(gitHubUrl) { UseShellExecute = true });

            var btnUpdate = new Button
                { AutoSize = true, Text = _localizationService.GetText("About.BtnUpdate", "检查更新") };
            btnUpdate.Click += async (_, _) => await CheckForUpdatesAsync(this);

            var btnClose = new Button
            {
                AutoSize = true, Text = _localizationService.GetText("About.BtnClose", "关闭"),
                DialogResult = DialogResult.OK
            };

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
            if (rtl)
            {
                // RTL：关闭 | 填充 | 检查更新 | GitHub
                btnLayout.Controls.Add(btnClose, 0, 0);
                btnLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0);
                btnLayout.Controls.Add(btnUpdate, 2, 0);
                btnLayout.Controls.Add(btnGitHub, 3, 0);
            }
            else
            {
                // LTR：GitHub | 检查更新 | 填充 | 关闭
                btnLayout.Controls.Add(btnGitHub, 0, 0);
                btnLayout.Controls.Add(btnUpdate, 1, 0);
                btnLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }, 2, 0);
                btnLayout.Controls.Add(btnClose, 3, 0);
            }

            buttonBar.Controls.Add(btnLayout);

            dlg.AcceptButton = btnClose;
            dlg.CancelButton = btnClose;
            // 在显示前对对话框整个控件树应用 RTL 设置
            RtlHelper.Apply(dlg, _localizationService.CurrentLanguage);
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
            if (_settings.MinimizeOnClose)
            {
                e.Cancel = true;
                Hide();
                _tray.BalloonTipTitle = _localizationService.GetText("Tray.StillRunning.Title", "仍在运行");
                _tray.BalloonTipText = _localizationService.GetText("Tray.StillRunning.Text", "已最小化到托盘，双击图标可恢复。");
                _tray.ShowBalloonTip(2000);
            }
            else
            {
                // 保存窗口位置与尺寸
                UpdateWindowBoundsToSettings(save: true);
                SaveTasks();
                SaveSettings();
                _tray.Visible = false;
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

                // 优先使用模型数据，避免解析本地化文本导致排序失效
                var tx = lvx.Tag as TaskItem;
                var ty = lvy.Tag as TaskItem;

                switch (col)
                {
                    // 开始时间列
                    case 2 when tx is not null && ty is not null:
                    {
                        var xs = tx.Start ?? DateTime.MinValue;
                        var ys = ty.Start ?? DateTime.MinValue;
                        var cmp = xs.CompareTo(ys);
                        return asc ? cmp : -cmp;
                    }
                    // 完成时间列
                    case 4 when tx is not null && ty is not null:
                    {
                        var cmp = tx.Finish.CompareTo(ty.Finish);
                        return asc ? cmp : -cmp;
                    }
                    // 持续时间列（以总秒数比较）
                    case 3 when tx is not null && ty is not null:
                    {
                        var xs = ToSeconds(tx);
                        var ys = ToSeconds(ty);
                        var cmp = xs.CompareTo(ys);
                        return asc ? cmp : -cmp;
                    }
                    // 剩余时间列（以总秒数比较，可能为负）；AwaitingAck 视为“到点”分组
                    case 5 when tx is not null && ty is not null:
                    {
                        var now = DateTime.Now;
                        var xDueGroup = tx.AwaitingAck || tx.Finish <= now;
                        var yDueGroup = ty.AwaitingAck || ty.Finish <= now;

                        // 先按“是否到点/等待确认”分组：升序时到点组在前，降序相反
                        if (xDueGroup != yDueGroup)
                        {
                            var cmpDue = xDueGroup ? -1 : 1;
                            return asc ? cmpDue : -cmpDue;
                        }

                        // 组内规则：
                        // - 到点/待确认：优先按账号名称升序，再按剩余秒数（点击列头切换时，仅反转“时间”比较，账号始终升序）
                        // - 未到点：保持原有行为，仅按剩余秒数
                        if (xDueGroup && yDueGroup)
                        {
                            var accCmp = string.Compare(tx.Account, ty.Account, StringComparison.Ordinal);
                            if (accCmp != 0) return accCmp;
                        }

                        var xs = (int)tx.Remaining.TotalSeconds;
                        var ys = (int)ty.Remaining.TotalSeconds;
                        var cmp = xs.CompareTo(ys);
                        return asc ? cmp : -cmp;
                    }
                }

                // 字符串比较
                var result = string.Compare(xText, yText, StringComparison.Ordinal);
                return asc ? result : -result;
            }

            private static int ToSeconds(TaskItem t)
            {
                var total = (long)t.Days * 24 * 3600 + (long)t.Hours * 3600 + (long)t.Minutes * 60;
                if (total <= 0) return 0;
                return (int)Math.Min(total, int.MaxValue);
            }
        }

        // ---------- 设置与持久化 ----------
        /// <summary>
        /// 读取设置并进行必要的兼容性修正（如首次运行、自启动与自动删除秒数的合理化），随后更新菜单勾选。
        /// </summary>
        private void LoadSettings()
        {
            _settings = _settingsStore.Load();

            // 首次运行：从注册表读取自启动状态并写入默认设置文件
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "settings.json")))
            {
                _settings.StartupOnBoot = _autostartManager.IsEnabled();
                SaveSettings();
            }

            if (_settings is { AutoDeleteCompletedSeconds: <= 0, AutoDeleteCompletedAfter1Min: true })
            {
                _settings.AutoDeleteCompletedSeconds = 60;
                SaveSettings();
            }

            // 合法性：不可为负
            if (_settings.AutoDeleteCompletedSeconds < 0)
            {
                _settings.AutoDeleteCompletedSeconds = 0;
                SaveSettings();
            }

            UpdateMenuChecks();
        }

        /// <summary>
        /// 将当前内存中的设置持久化到磁盘。
        /// </summary>
        private void SaveSettings() => _settingsStore.Save(_settings);

        /// <summary>
        /// 从存储加载任务列表到内存集合。
        /// </summary>
        private void LoadTasks()
        {
            _tasks.Clear();
            foreach (var t in _taskRepo.Load()) _tasks.Add(t);
        }

        /// <summary>
        /// 将当前任务集合保存到存储。
        /// </summary>
        private void SaveTasks() => _taskRepo.Save(_tasks);

        // ---------- 应用设置到界面 ----------
        /// <summary>
        /// 将应用设置同步到界面控件（账号、任务预设、字体等）。
        /// 在字体应用过程中若出现异常将忽略，确保主界面可继续显示。
        /// </summary>
        private void ApplySettingsToUi()
        {
            _cbAccount.Items.Clear();
            foreach (var a in _settings.Accounts) _cbAccount.Items.Add(a);
            if (_cbAccount.Items.Count == 0) _cbAccount.Items.Add(TaskItem.DefaultAccount);
            _cbAccount.SelectedIndex = 0;

            _cbTask.Items.Clear();
            foreach (var t in _settings.TaskPresets) _cbTask.Items.Add(t);

            try
            {
                var f = _settings.UiFont.ToFont();
                ApplyUiFont(f);
            }
            catch
            {
                /* ignore */
            }
        }

        /// <summary>
        /// 根据当前设置刷新菜单勾选状态（含自启动、关闭行为、自动删除、提前通知等）。
        /// </summary>
        private void UpdateMenuChecks()
        {
            _miAutoStart.Checked = _settings.StartupOnBoot;
            _miCloseExit.Checked = !_settings.MinimizeOnClose;
            _miCloseMinimize.Checked = _settings.MinimizeOnClose;
            UpdateAutoDeleteMenuChecks();
            UpdateAdvanceMenuChecks();
        }

        /// <summary>
        /// 根据当前设置更新“提前通知”菜单的勾选状态，并同步“同时准点通知”。
        /// </summary>
        private void UpdateAdvanceMenuChecks()
        {
            var secs = _settings.AdvanceNotifySeconds;
            var presets = new Dictionary<ToolStripMenuItem, int>
            {
                { _miAdvOff, 0 },
                { _miAdv30S, 30 },
                { _miAdv1M, 60 },
                { _miAdv3M, 180 },
                { _miAdv30M, 1800 },
                { _miAdv1H, 3600 }
            };

            foreach (var kv in presets)
                kv.Key.Checked = kv.Value == secs;

            // 自定义：当为正且不在预设中时勾选
            _miAdvCustom.Checked = secs > 0 && !presets.Values.Contains(secs);
            _miAdvAlsoDue.Checked = _settings.AlsoNotifyAtDue;
        }

        /// <summary>
        /// 设置“提前通知”秒数并保存，同时立即检查一次提醒并重排定时器。
        /// </summary>
        /// <param name="secs">提前秒数，若小于 0 则按 0 处理。</param>
        private void SetAdvanceSecondsAndSave(int secs)
        {
            if (secs < 0) secs = 0;
            _settings.AdvanceNotifySeconds = secs;
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
            var secs = _settings.AutoDeleteCompletedSeconds;
            var presets = new Dictionary<ToolStripMenuItem, int>
            {
                { _miDelOff, 0 },
                { _miDel30S, 30 },
                { _miDel1M, 60 },
                { _miDel3M, 180 },
                { _miDel30M, 1800 },
                { _miDel1H, 3600 }
            };

            foreach (var kv in presets)
                kv.Key.Checked = kv.Value == secs;

            _miDelCustom.Checked = secs > 0 && !presets.Values.Contains(secs);
        }

        /// <summary>
        /// 设置“已完成任务自动删除”秒数并保存，随后应用删除策略并尝试立即清理一次。
        /// </summary>
        /// <param name="secs">自动删除的延时秒数，若小于 0 则按 0 处理。</param>
        private void SetAutoDeleteSecondsAndSave(int secs)
        {
            if (secs < 0) secs = 0;
            _settings.AutoDeleteCompletedSeconds = secs;
            ApplyDeletionPolicyFromSettings();
            SaveSettings();
            UpdateAutoDeleteMenuChecks();
            // 变更后立即进行一次清理尝试（非强制），以尽快反映设置
            PurgePending(force: false);
        }

        /// <summary>
        /// 应用指定字体到整个窗口及子控件，并构造用于“删除线”显示的派生字体。
        /// </summary>
        /// <param name="f">要应用的字体。</param>
        private void ApplyUiFont(Font f)
        {
            Font = f;
            ApplyFontRecursive(this, f);
            _strikeFont?.Dispose();
            _strikeFont = new Font(f, f.Style | FontStyle.Strikeout);
        }

        /// <summary>
        /// 递归地将字体应用到所有子控件。
        /// </summary>
        /// <param name="parent">起始父控件。</param>
        /// <param name="f">字体。</param>
        private static void ApplyFontRecursive(Control parent, Font f)
        {
            foreach (Control c in parent.Controls)
            {
                c.Font = f;
                if (c.HasChildren) ApplyFontRecursive(c, f);
            }
        }

        /// <summary>
        /// 打开字体选择对话框并应用选择结果到界面与设置。
        /// </summary>
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
            _settings.UiFont = FontSpec.From(dlg.Font);
            SaveSettings();
        }

        // ---------- 托盘 ----------
        /// <summary>
        /// 初始化系统托盘图标与右键菜单，提供打开与退出操作，并处理双击恢复窗口。
        /// </summary>
        private void InitTray()
        {
            _tray.Icon = Icon;
            _tray.Text = _localizationService.GetText("Tray.Title", "升级提醒");
            _tray.Visible = true;
            _tray.DoubleClick += (_, _) =>
            {
                Show();
                Activate();
            };

            // 菜单样式优化
            _trayMenu.ShowImageMargin = false;
            _trayMenu.RenderMode = ToolStripRenderMode.System;

            // 状态区（只读项）
            _miStatHeader.Font = new Font(Font, FontStyle.Bold);

            // 绑定托盘操作项事件（一次性）
            _miTrayOpen.Click += (_, _) =>
            {
                Show();
                Activate();
            };
            _miTrayRefresh.Click += (_, _) =>
            {
                PurgePending(force: true);
                RefreshTable();
            };
            _miTrayCheckUpdate.Click += (_, _) => { _ = CheckForUpdatesAsync(this); };
            _miTrayOpenConfig.Click += (_, _) => OpenConfigFolder();
            _miTrayExit.Click += (_, _) =>
            {
                _settings.MinimizeOnClose = false;
                Close();
            };

            // 构建/重建托盘菜单（根据当前语言）
            RebuildTrayMenu();

            // 应用 RTL 布局到托盘菜单
            ApplyTrayRtlLayout();

            _tray.ContextMenuStrip = _trayMenu;
        }

        /// <summary>
        /// 依据当前语言重建托盘菜单（仅重排/更新项文本，复用项实例以避免重复绑定事件）。
        /// </summary>
        private void RebuildTrayMenu()
        {
            // 更新状态区与操作项文本
            _miStatHeader.Text = _localizationService.GetText("Tray.Status", "状态");
            _miTrayOpen.Text = _localizationService.GetText("Tray.Menu.Open", "打开(&O)");
            _miTrayRefresh.Text = _localizationService.GetText("Tray.Menu.Refresh", "刷新(&R)");
            _miTrayCheckUpdate.Text = _localizationService.GetText("Tray.Menu.CheckUpdate", "检查更新(&U)");
            _miTrayOpenConfig.Text = _localizationService.GetText("Tray.Menu.OpenConfigFolder", "打开配置文件夹(&F)");
            _miTrayExit.Text = _localizationService.GetText("Tray.Menu.Exit", "退出(&X)");

            // 清空并按顺序重新添加
            _trayMenu.Items.Clear();
            _trayMenu.Items.Add(_miStatHeader);
            _trayMenu.Items.Add(_miStatTotal);
            _trayMenu.Items.Add(_miStatDue);
            _trayMenu.Items.Add(_miStatPending);
            _trayMenu.Items.Add(_miStatNext);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_miTrayOpen);
            _trayMenu.Items.Add(_miTrayRefresh);
            _trayMenu.Items.Add(_miTrayCheckUpdate);
            _trayMenu.Items.Add(_miTrayOpenConfig);
            _trayMenu.Items.Add(_miTrayExit);

            // 同步状态区文本
            UpdateTrayMenuStatus();
        }

        // ---------- 列表 ----------
        /// <summary>
        /// 刷新任务列表内容：根据内存中的 <see cref="_tasks"/> 重建 <see cref="_listView"/> 的行与样式。
        /// </summary>
        private void RefreshTable()
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var t in _tasks)
            {
                var duration = _durationFormatter?.Format(t.Days, t.Hours, t.Minutes) ?? "";
                var repeatText = t.Repeat is { IsRepeat: true } r
                    ? FormatRepeatSpec(r) + (t.RepeatCount > 0
                        ? "，" + _localizationService.GetFormattedText("Status.RepeatedTimes", t.RepeatCount)
                        : "")
                    : "";
                var actionText = t.AwaitingAck
                    ? _localizationService.GetText("Action.Acknowledge", "确认完成")
                    : (t.Done
                        ? _localizationService.GetText("Action.UndoComplete", "撤销完成")
                        : _localizationService.GetText("Action.Complete", "完成"));
                var startText = t.Start.HasValue
                    ? _dateFormat.FormatDateTime(t.Start.Value, includeYear: true,
                        includeSeconds: t.Start.Value.Second != 0)
                    : string.Empty;
                var finishText =
                    _dateFormat.FormatDateTime(t.Finish, includeYear: true, includeSeconds: t.Finish.Second != 0);

                var it = new ListViewItem(t.Account)
                {
                    SubItems =
                    {
                        t.TaskName,
                        startText,
                        duration,
                        finishText,
                        GetRemainingText(t),
                        repeatText,
                        actionText,
                        t.PendingDelete
                            ? _localizationService.GetText("Action.UndoDelete", "撤销删除")
                            : _localizationService.GetText("Action.Delete", "删除")
                    },
                    Tag = t
                };
                _listView.Items.Add(it);
            }

            RepaintStyles();
            _listView.EndUpdate();

            if (_sortMode == SortMode.Custom)
            {
                _listView.ListViewItemSorter = new ListViewItemComparer(_customSortColumn, _customSortAsc);
                _listView.Sort();
            }

            AdjustListViewColumns();
            UpdateStatusBar();
            UpdateListViewSortArrow();

            // 异步清除焦点项，避免在刷新/排序后出现虚线焦点框
            if (IsHandleCreated)
            {
                BeginInvoke(() =>
                {
                    if (_listView.SelectedIndices.Count != 0) return;
                    ClearListViewFocusRegardlessOfSelection();
                    // 若列表当前仍持有焦点，则把焦点移出以彻底避免焦点框
                    if (_listView.Focused)
                        ActiveControl = null;
                });
            }
        }

        /// <summary>
        /// 遍历列表行，按任务的当前状态刷新“剩余时间”单元格文本。
        /// </summary>
        private void UpdateRemainingCells()
        {
            foreach (ListViewItem row in _listView.Items)
            {
                if (row.Tag is TaskItem t)
                    row.SubItems[5].Text = GetRemainingText(t);
            }
        }

        /// <summary>
        /// 计算“剩余时间”列的显示文本：
        /// - 若任务已提醒且等待确认，且未启用“提醒后暂停计时”，显示“到点”。
        /// - 否则显示真实剩余时间（本地化）。
        /// </summary>
        private string GetRemainingText(TaskItem t)
        {
            var pauseUntilDone = t.Repeat?.IsPauseUntilDone == true;
            var now = DateTime.Now;
            var isDue = t.Finish <= now;
            if (isDue || (t.AwaitingAck && !pauseUntilDone))
                return _localizationService.GetText("Remaining.Due", "到点");

            return _durationFormatter?.Format(t.Remaining.Days, t.Remaining.Hours,
                t.Remaining.Minutes, t.Remaining.Seconds, true) ?? "";
        }

        /// <summary>
        /// 按任务状态（待删除、已完成、到点/等待确认）刷新列表行的前景/背景色与字体样式。
        /// </summary>
        private void RepaintStyles()
        {
            foreach (ListViewItem row in _listView.Items)
            {
                if (row.Tag is not TaskItem t) continue;

                row.BackColor = Color.White;
                row.ForeColor = Color.Black;
                row.Font = Font;

                if (t.PendingDelete && _strikeFont is not null)
                {
                    row.Font = _strikeFont;
                }
                else if (t.Done)
                {
                    row.ForeColor = Color.Gray;
                    row.BackColor = Color.White;
                }
                else if (t is { AwaitingAck: true } || t.Finish <= DateTime.Now)
                {
                    row.BackColor = DueBackColor;
                }

                row.SubItems[7].Text = t.AwaitingAck
                    ? _localizationService.GetText("Action.Acknowledge", "确认完成")
                    : (t.Done
                        ? _localizationService.GetText("Action.UndoComplete", "撤销完成")
                        : _localizationService.GetText("Action.Complete", "完成"));
                row.SubItems[8].Text = t.PendingDelete
                    ? _localizationService.GetText("Action.UndoDelete", "撤销删除")
                    : _localizationService.GetText("Action.Delete", "删除");
            }

            UpdateStatusBar();
        }

        /// <summary>
        /// 处理列表点击：
        /// - 第7列（操作1）：完成/撤销或“确认完成”（暂停模式）。
        /// - 第8列（操作2）：标记/撤销删除。
        /// - 其他前6列：将该行任务载入编辑区。
        /// 操作后自动刷新列表并重排定时器。
        /// </summary>
        private void HandleListClick()
        {
            var hit = _listView.PointToClient(MousePosition);
            var info = _listView.HitTest(hit);
            if (info.Item is null) return;
            var sub = info.Item.SubItems.IndexOf(info.SubItem);

            if (info.Item.Tag is not TaskItem t) return;

            switch (sub)
            {
                case 7:
                    if (t.AwaitingAck)
                    {
                        // 确认本次提醒已处理
                        t.AwaitingAck = false;
                        t.Notified = false;
                        t.AdvanceNotified = false;
                        if (t.Repeat is { IsRepeat: true, IsPauseUntilDone: true } spec)
                        {
                            // 暂停模式：从“现在”起开始下一次计时，并跨越跳过段
                            t.RepeatCount++;
                            var first = CalcNextOccurrence(DateTime.Now, spec);
                            var baseNext = CalcNextEffectiveOccurrence(first, spec, t.RepeatCursor, out var adv);
                            var nextEff = ApplyRepeatOffset(DateTime.Now, baseNext, spec);
                            if (spec is { EndAt: not null } && nextEff > spec.EndAt!.Value)
                            {
                                t.Repeat = new RepeatSpec { Mode = RepeatMode.None };
                            }
                            else
                            {
                                t.Finish = nextEff;
                                t.RepeatCursor += adv;
                            }
                        }
                        if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
                        SaveTasks();
                        RefreshTable();
                        RescheduleNextTick();
                        break;
                    }

                    t.Done = !t.Done;
                    t.CompletedTime = t.Done ? DateTime.Now : null;
                    if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
                    SaveTasks();
                    RefreshTable();
                    RescheduleNextTick();
                    break;

                case 8:
                    t.PendingDelete = !t.PendingDelete;
                    t.DeleteMarkTime = t.PendingDelete ? DateTime.Now : null;
                    if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
                    SaveTasks();
                    RefreshTable();
                    RescheduleNextTick();
                    break;

                case >= 0 and < 6:
                    _cbAccount.Text = t.Account;
                    _cbTask.Text = t.TaskName;
                    _numDays.Value = t.Days;
                    _numHours.Value = t.Hours;
                    _numMinutes.Value = t.Minutes;

                    RecalcFinishFromFields();
                    break;
            }
        }

        // ---------- 新增任务 ----------
        /// <summary>
        /// 基于开始时间与“天/时/分”输入，重新计算完成时间并更新显示。
        /// </summary>
        private void RecalcFinishFromFields()
        {
            var st = _dtpStart.Value;
            var d = (int)_numDays.Value;
            var h = (int)_numHours.Value;
            var m = (int)_numMinutes.Value;

            var fin = st.AddDays(d).AddHours(h).AddMinutes(m);
            _tbFinish.Text = fin.ToString(TaskItem.TimeFormat);
        }

        /// <summary>
        /// 获取当前选择的账号文本；若未选择或为空则返回默认账号。
        /// </summary>
        /// <returns>账号名称。</returns>
        private string GetAccountText()
        {
            if (_cbAccount.SelectedItem is string s && !string.IsNullOrWhiteSpace(s)) return s;
            return TaskItem.DefaultAccount;
        }

        /// <summary>
        /// 新增或保存任务：
        /// - 若选中了列表项，则编辑并覆盖该任务（保留重复设置但重置计数）。
        /// - 否则按表单输入新增任务（使用当前默认重复设置）。
        /// 完成后保存并刷新界面。
        /// </summary>
        private void AddOrSaveTask()
        {
            var acc = GetAccountText();
            var taskName = string.IsNullOrWhiteSpace(_cbTask.Text) ? "-" : _cbTask.Text.Trim();
            var st = _dtpStart.Value;

            var d = (int)_numDays.Value;
            var h = (int)_numHours.Value;
            var m = (int)_numMinutes.Value;

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
            var selectedTask = _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag as TaskItem : null;
            if (selectedTask is not null)
            {
                // 编辑任务：沿用 Repeat 设置，但按规则“编辑后清零计数”
                t.Repeat = selectedTask.Repeat;
                t.RepeatCount = 0;
                var idx = _tasks.IndexOf(selectedTask);
                if (idx >= 0)
                {
                    _tasks[idx] = t;
                    if (_sortMode == SortMode.DefaultByFinish)
                    {
                        var moved = _tasks[idx];
                        _tasks.RemoveAt(idx);
                        _sortStrategy.Insert(_tasks, moved);
                    }
                }
            }
            else
            {
                // 新增任务应用当前的“重复设置”默认值
                t.Repeat = _currentRepeatSpec;
                t.RepeatCount = 0;
                if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Insert(_tasks, t);
                else _tasks.Add(t);
            }

            SaveTasks();
            RefreshTable();
            UpdateStatusBar();
        }

        /// <summary>
        /// 删除所有已完成的任务。
        /// </summary>
        private void DeleteAllDone()
        {
            for (var i = 0; i < _tasks.Count;)
            {
                if (_tasks[i].Done) _tasks.RemoveAt(i);
                else i++;
            }
        }

        // ---------- 到点提醒 ----------
        /// <summary>
        /// 检查任务的提前/到点提醒触发条件并弹出通知；根据重复规则推进发生指针与完成时间。
        /// 同时处理“跳过段”“暂停直到确认”“截止时间”等规则。
        /// 发生变化时保存并刷新列表，然后重排定时器与状态栏。
        /// </summary>
        private void CheckDueAndNotify()
        {
            var changed = false;
            var now = DateTime.Now;
            // 收集合并的“提前/到点”通知，避免同一时刻弹出过多气泡
            var advToasts = new List<(string Title, string Body)>();
            var dueToasts = new List<(string Title, string Body)>();
            foreach (var t in _tasks)
            {
                // 已完成或待删除的任务不再参与提醒
                if (t is { PendingDelete: true } || t is { Done: true }) continue;
                var spec = t.Repeat;
                var isRepeat = spec?.IsRepeat == true;
                // 暂停等待用户确认的任务（仅当开启“提醒后暂停”时）不再推进或重复弹窗
                if (t.AwaitingAck && isRepeat && spec!.PauseUntilDone) continue;

                // 若设置了截止且当前 Finish 已超过截止，立即停止后续提醒
                if (isRepeat && spec!.HasEnd && t.Finish > spec.EndAt!.Value)
                {
                    t.Repeat = new RepeatSpec { Mode = RepeatMode.None };
                    t.AdvanceNotified = true; // 阻止未来的提前提醒
                    t.Notified = true; // 阻止未来的到点提醒
                    changed = true;
                    continue;
                }

                // 提前提醒（仅对非跳过周期触发）
                var adv = _settings.AdvanceNotifySeconds;
                if (adv > 0 && !t.AdvanceNotified && t.Finish > now)
                {
                    var advTime = t.Finish.AddSeconds(-adv);
                    var inSkipPhase = isRepeat && ShouldSkipOccurrence(spec!, t.RepeatCursor);
                    if (!inSkipPhase && advTime <= now)
                    {
                        var title = string.Format(_localizationService.GetText("Toast.Advance.Title", "[提前] {0}"),
                            t.Account);
                        var body = string.Format(
                            _localizationService.GetText("Toast.Advance.Body", "{0} 即将到点，完成时间：{1}"), t.TaskName,
                            t.FinishStr);
                        advToasts.Add((title, body));
                        t.AdvanceNotified = true;
                        changed = true;
                    }
                }

                // 到点检查
                if (t.Finish > now || t.Notified) continue;

                var skipThis = isRepeat && ShouldSkipOccurrence(spec!, t.RepeatCursor);

                if (skipThis)
                {
                    // 跳过阶段：不弹窗、不计数，直接推进到下一次“会提醒”的发生
                    t.Notified = true;
                    changed = true;
                    if (isRepeat)
                    {
                        var baseNext = CalcNextEffectiveOccurrence(t.Finish, spec!, t.RepeatCursor, out var advanced);
                        var nextEff = ApplyRepeatOffset(t.Finish, baseNext, spec!);
                        if (spec!.HasEnd && nextEff > spec.EndAt!.Value)
                        {
                            t.Repeat = new RepeatSpec { Mode = RepeatMode.None };
                        }
                        else
                        {
                            t.Finish = nextEff;
                            t.Notified = false;
                            t.AdvanceNotified = false;
                        }

                        t.RepeatCursor += advanced;
                    }

                    continue;
                }

                // 非跳过阶段
                var showDueToast = _settings.AlsoNotifyAtDue || !t.AdvanceNotified;
                if (showDueToast)
                {
                    var title = string.Format(_localizationService.GetText("Toast.Due.Title", "[到点] {0}"), t.Account);
                    var body = string.Format(_localizationService.GetText("Toast.Due.Body", "{0} 完成时间：{1}"), t.TaskName,
                        t.FinishStr);
                    dueToasts.Add((title, body));
                }

                t.Notified = true;
                changed = true;

                if (isRepeat && spec!.PauseUntilDone)
                {
                    // 暂停计时：等待用户确认，保持 Finish 不变（显示“到点”并高亮）
                    t.AwaitingAck = true;
                    // 本次“发生”已完成（提醒触发），推进总光标
                    t.RepeatCursor++;
                }
                else if (isRepeat)
                {
                    // 正常模式：到点后仍然高亮等待确认，但计时立即进入“下一次会提醒”的时点
                    t.AwaitingAck = true;
                    // 发生光标+1（本次发生）与提醒计数+1
                    t.RepeatCursor++;
                    t.RepeatCount++;
                    // 基于下一次发生时间，跨越所有处于跳过段的发生
                    var first = CalcNextOccurrence(t.Finish, spec!);
                    var baseNext = CalcNextEffectiveOccurrence(first, spec!, t.RepeatCursor, out var adv2);
                    var nextEff = ApplyRepeatOffset(t.Finish, baseNext, spec!);
                    if (spec!.HasEnd && nextEff > spec.EndAt!.Value)
                    {
                        t.Repeat = new RepeatSpec { Mode = RepeatMode.None };
                    }
                    else
                    {
                        t.Finish = nextEff;
                        t.Notified = false;
                        t.AdvanceNotified = false;
                        t.RepeatCursor += adv2; // 累计被跳过的发生
                    }
                }
            }

            // 统一发送“提前”通知：如同时有多条，则合并为一条
            if (advToasts.Count > 0)
            {
                if (advToasts.Count >= 2)
                {
                    var titleAggA = _localizationService.GetText("Toast.Advance.Aggregated.Title", "[提前] 提醒");
                    var bodyAggA = string.Format(_localizationService.GetText("Toast.Advance.Aggregated.Body", "{0} 项任务即将到点"), advToasts.Count);
                    _notifier.Toast(titleAggA, bodyAggA);
                }
                else
                {
                    foreach (var (title, body) in advToasts)
                    {
                        _notifier.Toast(title, body);
                    }
                }
            }

            // 统一发送“到点”通知：如同时有多条，则合并为一条
            if (dueToasts.Count > 0)
            {
                if (dueToasts.Count >= 2)
                {
                    var titleAgg = _localizationService.GetText("Toast.Due.Aggregated.Title", "到点提醒");
                    var bodyAgg = string.Format(_localizationService.GetText("Toast.Due.Aggregated.Body", "{0} 项任务已到点"), dueToasts.Count);
                    _notifier.Toast(titleAgg, bodyAgg);
                }
                else
                {
                    foreach (var (title, body) in dueToasts)
                    {
                        _notifier.Toast(title, body);
                    }
                }
            }

            if (changed)
            {
                // 当任务到点/进入等待确认或被推进到下一发生时，默认排序应重排，
                // 以便“到点/待确认”组内按账号名称优先显示
                if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
                SaveTasks();
                RefreshTable(); // 刷新以更新“完成时间/重复”列文本与已重复次数
            }

            RescheduleNextTick();
            UpdateStatusBar();
        }

        /// <summary>
        /// 自适应调度：根据下一次提醒时间点（包含提前提醒与到点）计算计时器间隔，
        /// 在固定“提前量”前唤醒并将间隔夹紧到安全范围，避免过于频繁或过久的轮询。
        /// </summary>
        private void RescheduleNextTick()
        {
            try
            {
                var now = DateTime.Now;
                DateTime? next = null;
                var adv = _settings.AdvanceNotifySeconds;

                foreach (var t in _tasks)
                {
                    // 已完成或待删除的任务不参与后续调度
                    if (t is { PendingDelete: true } || t is { Done: true }) continue;
                    // 等待确认的任务不推进调度（保持“到点”）
                    if (t.AwaitingAck) continue;

                    var spec = t.Repeat;
                    var isRepeat = spec?.IsRepeat == true;
                    var inSkipPhase = isRepeat && ShouldSkipOccurrence(spec!, t.RepeatCursor);
                    var targetFinish = inSkipPhase
                        ? ApplyRepeatOffset(t.Finish, CalcNextEffectiveOccurrence(t.Finish, spec!, t.RepeatCursor, out _), spec!)
                        : t.Finish;

                    // 候选1：提前提醒时间点（若启用且尚未提前提醒）
                    if (adv > 0 && !t.AdvanceNotified)
                    {
                        var advTime = targetFinish.AddSeconds(-adv);
                        if (advTime > now) next = next is null || advTime < next ? advTime : next;
                    }

                    // 候选2：到点提醒时间（遵循 AlsoNotifyAtDue 设置）
                    if (!t.Notified)
                    {
                        if (!_settings.AlsoNotifyAtDue && t.AdvanceNotified)
                        {
                            // 已提前提醒且关闭了“同时准点通知” -> 跳过到点提醒的调度
                        }
                        else if (targetFinish > now)
                        {
                            next = next is null || targetFinish < next ? targetFinish : next;
                        }
                    }
                }

                const int minMs = 1000; // 最小间隔 1 秒
                const int maxMs = 5000; // 最大间隔 5 秒（避免等待过久）
                const int guardSec = 3; // 提前量 3 秒（稍早唤醒以对冲计时抖动）

                var interval = maxMs;
                if (next.HasValue)
                {
                    var target = next.Value.AddSeconds(-guardSec);
                    if (target < now) target = now.AddMilliseconds(minMs); // 若已过期，则尽快（按最小间隔）检查
                    var deltaMs = (int)Math.Max(0, (target - now).TotalMilliseconds);
                    interval = Math.Clamp(deltaMs, minMs, maxMs);
                }

                _timerTick.Interval = interval;
            }
            catch
            {
                // 忽略
            }
        }

        // ---------- 重复调度辅助 ----------
        /// <summary>
        /// 基于重复模式计算给定发生时间的下一次发生时间（不考虑“跳过段”和“暂停”）。
        /// </summary>
        /// <param name="current">当前发生时间。</param>
        /// <param name="spec">重复规则。</param>
        /// <returns>下一次发生时间。</returns>
        private static DateTime CalcNextOccurrence(DateTime current, RepeatSpec spec)
        {
            var next = current;
            switch (spec.Mode)
            {
                case RepeatMode.Daily:
                    next = current.AddDays(1);
                    break;
                case RepeatMode.Weekly:
                    next = current.AddDays(7);
                    break;
                case RepeatMode.Monthly:
                    next = current.AddMonths(1);
                    break;
                case RepeatMode.Yearly:
                    next = current.AddYears(1);
                    break;
                case RepeatMode.Custom:
                    var c = spec.Custom;
                    if (c is not null)
                    {
                        next = next.AddYears(Math.Max(0, c.Years));
                        next = next.AddMonths(Math.Max(0, c.Months));
                        next = next.AddDays(Math.Max(0, c.Days));
                        next = next.AddHours(Math.Max(0, c.Hours));
                        next = next.AddMinutes(Math.Max(0, c.Minutes));
                        next = next.AddSeconds(Math.Max(0, c.Seconds));
                    }

                    break;
                default:
                    next = current;
                    break;
            }

            // 安全兜底，避免不前进导致死循环
            if (next <= current) next = current.AddSeconds(1);
            return next;
        }

        // 计算下一次“会实际提醒”的时间（跳过所有处于 Skip 段的发生），不改变 RepeatCount；返回跳过的发生次数 advanced
        private static DateTime CalcNextEffectiveOccurrence(DateTime current, RepeatSpec spec, int repeatCursor,
            out int advanced)
        {
            var next = current;
            advanced = 0;
            // 逐个推进 occurrence，直到来到一个需要提醒的 occurrence
            var guard = 0;
            while (ShouldSkipOccurrence(spec, repeatCursor) && guard < 1000)
            {
                next = CalcNextOccurrence(next, spec);
                repeatCursor++;
                advanced++;
                guard++;
            }

            return next;
        }

        // 应用“提醒后偏移”到下一次发生时间；并做防回退保护：不得早于基准时间的下一秒
        private static DateTime ApplyRepeatOffset(DateTime baseline, DateTime nextBase, RepeatSpec spec)
        {
            var next = nextBase;
            var off = spec.OffsetAfterSeconds;
            if (off != 0)
            {
                next = next.AddSeconds(off);
            }
            if (next <= baseline) next = baseline.AddSeconds(1);
            return next;
        }

        private static bool ShouldSkipOccurrence(RepeatSpec spec, int repeatCountSoFar)
        {
            if (spec is not { HasSkip: true, Skip: not null }) return false;
            var a = Math.Max(0, spec.Skip.RemindTimes);
            var b = Math.Max(0, spec.Skip.SkipTimes);
            var l = a + b;
            if (a == 0 || b == 0 || l == 0) return false;
            var idx = repeatCountSoFar % l; // 0..l-1
            return idx >= a; // 0..a-1: 提醒；a..l-1: 跳过
        }

        private void PurgePending(bool force)
        {
            var changed = false;
            var now = DateTime.Now;

            for (var i = 0; i < _tasks.Count;)
            {
                var t = _tasks[i];
                if (_deletionPolicy.ShouldRemove(t, now, force))
                {
                    _tasks.RemoveAt(i);
                    changed = true;
                }
                else i++;
            }

            if (!changed) return;

            if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
            SaveTasks();
            RefreshTable();
        }

        // ---------- 管理列表 ----------
        private void ShowManager(bool isAccount)
        {
            var title = isAccount
                ? _localizationService.GetText("UI.ManageAccount", "账号管理")
                : _localizationService.GetText("UI.ManageTask", "任务管理");
            using var dlg = new ManageListForm(title, isAccount,
                isAccount ? _settings.Accounts : _settings.TaskPresets, _localizationService);
            // 实时保存
            dlg.ItemsChanged += (_, e) =>
            {
                if (isAccount)
                {
                    var prev = _cbAccount.SelectedItem?.ToString();
                    _settings.Accounts = e.Items;
                    _cbAccount.Items.Clear();
                    foreach (var a in _settings.Accounts) _cbAccount.Items.Add(a);
                    if (_cbAccount.Items.Count == 0) _cbAccount.Items.Add(TaskItem.DefaultAccount);
                    if (!string.IsNullOrEmpty(prev) && _cbAccount.Items.Contains(prev))
                        _cbAccount.SelectedItem = prev;
                    else if (_cbAccount.Items.Count > 0 && _cbAccount.SelectedIndex < 0)
                        _cbAccount.SelectedIndex = 0;
                }
                else
                {
                    var text = _cbTask.Text;
                    var selStart = _cbTask.SelectionStart;
                    var selLength = _cbTask.SelectionLength;
                    _settings.TaskPresets = e.Items;
                    _cbTask.Items.Clear();
                    foreach (var t in _settings.TaskPresets) _cbTask.Items.Add(t);
                    // 恢复输入体验
                    _cbTask.Text = text;
                    if (selStart >= 0 && selStart <= _cbTask.Text.Length)
                    {
                        _cbTask.SelectionStart = selStart;
                        _cbTask.SelectionLength = selLength;
                    }
                }

                SaveSettings();
            };
            // 重命名联动：更新现有任务中的账号/任务名，并持久化到 tasks.json
            dlg.ItemEdited += (_, e) =>
            {
                var oldName = e.OldName;
                var newName = e.NewName;
                var changedAny = false;

                for (int i = 0; i < _tasks.Count; i++)
                {
                    var t = _tasks[i];
                    if (isAccount)
                    {
                        if (!string.Equals(t.Account, oldName, StringComparison.Ordinal)) continue;
                        var nt = new TaskItem
                        {
                            Account = newName,
                            TaskName = t.TaskName,
                            Start = t.Start,
                            Days = t.Days,
                            Hours = t.Hours,
                            Minutes = t.Minutes,
                            Finish = t.Finish,
                            Notified = t.Notified,
                            AdvanceNotified = t.AdvanceNotified,
                            AwaitingAck = t.AwaitingAck,
                            Done = t.Done,
                            CompletedTime = t.CompletedTime,
                            PendingDelete = t.PendingDelete,
                            DeleteMarkTime = t.DeleteMarkTime,
                            Repeat = t.Repeat,
                            RepeatCount = t.RepeatCount,
                            RepeatCursor = t.RepeatCursor
                        };
                        _tasks[i] = nt;
                        changedAny = true;
                    }
                    else
                    {
                        if (!string.Equals(t.TaskName, oldName, StringComparison.Ordinal)) continue;
                        var nt = new TaskItem
                        {
                            Account = t.Account,
                            TaskName = newName,
                            Start = t.Start,
                            Days = t.Days,
                            Hours = t.Hours,
                            Minutes = t.Minutes,
                            Finish = t.Finish,
                            Notified = t.Notified,
                            AdvanceNotified = t.AdvanceNotified,
                            AwaitingAck = t.AwaitingAck,
                            Done = t.Done,
                            CompletedTime = t.CompletedTime,
                            PendingDelete = t.PendingDelete,
                            DeleteMarkTime = t.DeleteMarkTime,
                            Repeat = t.Repeat,
                            RepeatCount = t.RepeatCount,
                            RepeatCursor = t.RepeatCursor
                        };
                        _tasks[i] = nt;
                        changedAny = true;
                    }
                }

                if (changedAny)
                {
                    if (_sortMode == SortMode.DefaultByFinish) _sortStrategy.Sort(_tasks);
                    SaveTasks();
                    RefreshTable();
                }
            };
            dlg.ShowDialog(this);
        }

        // ---------- 开机自启 ----------
        private void ToggleAutostart()
        {
            _settings.StartupOnBoot = !_settings.StartupOnBoot;
            try
            {
                _autostartManager.SetEnabled(_settings.StartupOnBoot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localizationService.GetFormattedText("Error.FailedToSetAutostart", ex.Message),
                    _localizationService.GetText("Error.Title", "错误"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            var keepSecs = _settings.AutoDeleteCompletedSeconds > 0
                ? _settings.AutoDeleteCompletedSeconds
                : int.MaxValue;
            _deletionPolicy = new SimpleDeletionPolicy(pendingDeleteDelaySeconds: 3, completedKeepSeconds: keepSecs);
        }

        // ---------- 窗口记忆 ----------
        private void RestoreWindowBoundsFromSettings()
        {
            try
            {
                if (_settings is { WindowWidth: > 0, WindowHeight: > 0 })
                {
                    var bounds = new Rectangle(
                        _settings.WindowX >= 0 ? _settings.WindowX : Location.X,
                        _settings.WindowY >= 0 ? _settings.WindowY : Location.Y,
                        _settings.WindowWidth,
                        _settings.WindowHeight);

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

                if (_settings.WindowMaximized)
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
                _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
                if (WindowState == FormWindowState.Normal)
                {
                    _settings.WindowX = Bounds.X;
                    _settings.WindowY = Bounds.Y;
                    _settings.WindowWidth = Bounds.Width;
                    _settings.WindowHeight = Bounds.Height;
                }

                if (save) SaveSettings();
            }
            catch
            {
                /* ignore */
            }
        }

        /// <summary>
        /// 清除已保存的窗口位置与尺寸，并将当前窗口恢复到默认大小并居中。
        /// </summary>
        private void ResetWindowToDefault()
        {
            try
            {
                // 1) 清空保存字段并立即持久化
                _settings.WindowX = -1;
                _settings.WindowY = -1;
                _settings.WindowWidth = -1;
                _settings.WindowHeight = -1;
                _settings.WindowMaximized = false;
                SaveSettings();

                // 2) 恢复到默认窗口大小与位置
                if (WindowState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Normal;

                var totalWidth = AccountColWidth + TaskColWidth + StartTimeColWidth + DurationColWidth +
                                 FinishTimeColWidth + RemainingTimeColWidth + RepeatColWidth + (ActionColWidth * 2) +
                                 ExtraSpace;
                var defaultHeight = (int)Math.Round(totalWidth * InvPhi);
                ClientSize = new Size(totalWidth, defaultHeight);

                // 居中到屏幕
                CenterToScreen();

                // 根据新尺寸调整列表列宽
                AdjustListViewColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localizationService.GetFormattedText("Error.FailedToResetWindowSize", ex.Message),
                    _localizationService.GetText("Error.Title", "错误"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                    if (MessageBox.Show(owner,
                            _localizationService.GetText("Update.CheckFailedGetInfo", "无法从 GitHub 获取最新版本信息，是否打开发布页？"),
                            _localizationService.GetText("Update.CheckUpdateTitle", "检查更新"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning) is DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(releasesPage) { UseShellExecute = true });
                    }

                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var latest = JsonSerializer.Deserialize<GitHubLatestRelease>(json);
                if (latest is null || string.IsNullOrWhiteSpace(latest.TagName))
                {
                    MessageBox.Show(owner,
                        _localizationService.GetText("Update.ParseFailed", "未能解析最新版本信息。"),
                        _localizationService.GetText("Update.CheckUpdateTitle", "检查更新"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var tag = latest.TagName.Trim();
                var normalized = tag.StartsWith('v') ? tag[1..] : tag;

                if (!Version.TryParse(normalized, out var latestVer))
                {
                    if (MessageBox.Show(owner,
                            _localizationService.GetFormattedText("Update.InvalidVersionFormat", tag),
                            _localizationService.GetText("Update.CheckUpdateTitle", "检查更新"),
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) != DialogResult.Yes)
                    {
                        return;
                    }

                    var url = string.IsNullOrWhiteSpace(latest.HtmlUrl) ? releasesPage : latest.HtmlUrl;
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return;
                }

                latestVer = NormalizeVersion(latestVer);

                if (latestVer > NormalizeVersion(GetCurrentVersion()))
                {
                    var msg = _localizationService.GetFormattedText("Update.NewVersionFound",
                        FormatVersionForDisplay(latestVer),
                        FormatVersionForDisplay(GetCurrentVersion()));
                    if (!openOnNew ||
                        MessageBox.Show(owner, msg,
                            _localizationService.GetText("Update.UpdateAvailableTitle", "有可用更新"),
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) ==
                        DialogResult.Yes)
                    {
                        var url = string.IsNullOrWhiteSpace(latest.HtmlUrl) ? releasesPage : latest.HtmlUrl;
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show(owner,
                        _localizationService.GetFormattedText("Update.AlreadyLatest",
                            FormatVersionForDisplay(GetCurrentVersion())),
                        _localizationService.GetText("Update.CheckUpdateTitle", "检查更新"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(owner,
                        _localizationService.GetFormattedText("Update.CheckFailedGeneral", ex.Message),
                        _localizationService.GetText("Update.CheckUpdateTitle", "检查更新"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error) is DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(releasesPage) { UseShellExecute = true });
                }
            }
        }
    }
}