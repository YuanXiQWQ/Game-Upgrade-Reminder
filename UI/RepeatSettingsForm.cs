/*
 * 重复设置窗口
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 创建日期: 2025-08-21
 * 最后修改: 2025-08-21
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Models;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 重复设置窗口（UI）。
    /// - 单选：每天/每周/每月/每年/自定义（含“不重复”）
    /// - 自定义周期输入：年/月/日/时/分/秒（≥0，至少一项>0）
    /// - 结束时间：日期 + 时/分/秒
    /// - 跳过提醒：每提醒A次，跳过B次
    ///
    /// 提供 UI 与 <see cref="CurrentSpec"/> 的读写，并通过 <see cref="RepeatSpecChanged"/> 事件实现“变更即保存”（含 250ms 防抖）。
    /// </summary>
    internal sealed class RepeatSettingsForm : Form
    {
        // 模式选择
        private readonly RadioButton _rbNone = new() { Text = "不重复", AutoSize = true };
        private readonly RadioButton _rbDaily = new() { Text = "每天", AutoSize = true };
        private readonly RadioButton _rbWeekly = new() { Text = "每周", AutoSize = true };
        private readonly RadioButton _rbMonthly = new() { Text = "每月", AutoSize = true };
        private readonly RadioButton _rbYearly = new() { Text = "每年", AutoSize = true };
        private readonly RadioButton _rbCustom = new() { Text = "自定义", AutoSize = true };

        // 自定义周期
        private readonly NumericUpDown _numYears = new() { Minimum = 0, Maximum = 1000, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numMonths = new() { Minimum = 0, Maximum = 1200, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numDays = new() { Minimum = 0, Maximum = 365000, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numHours = new() { Minimum = 0, Maximum = 100000, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numMinutes = new() { Minimum = 0, Maximum = 100000, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numSeconds = new() { Minimum = 0, Maximum = 100000, Width = 60, Anchor = AnchorStyles.Left };

        // 结束时间
        private readonly DateTimePicker _dtEndDate = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            ShowUpDown = false,
            Width = 120,
            Anchor = AnchorStyles.Left
        };

        private readonly NumericUpDown _endHour = new() { Minimum = 0, Maximum = 23, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _endMinute = new() { Minimum = 0, Maximum = 59, Width = 60, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _endSecond = new() { Minimum = 0, Maximum = 59, Width = 60, Anchor = AnchorStyles.Left };

        // 结束时间可选：无（默认）
        private readonly CheckBox _chkNoEnd = new() { Text = "无", AutoSize = true, Anchor = AnchorStyles.Left };

        // 跳过规则
        private readonly NumericUpDown _numRemindTimes = new() { Minimum = 0, Maximum = 100000, Width = 80, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown _numSkipTimes = new() { Minimum = 0, Maximum = 100000, Width = 80, Anchor = AnchorStyles.Left };

        // 提醒后暂停直到用户确认
        private readonly CheckBox _chkPause = new() { Text = "提醒后暂停计时，直到确认", AutoSize = true, Anchor = AnchorStyles.Left };

        // 校验与提示
        private readonly Label _lblHint = new() { AutoSize = true };

        /// <summary>
        /// 当前编辑的重复设置。读取时根据 UI 组合，设置时回填 UI。
        /// null 或 Mode=None 视为“不重复”。
        /// </summary>
        public RepeatSpec? CurrentSpec
        {
            get => _BuildSpecFromUI();
            set => _ApplySpecToUI(value);
        }

        // 变更即保存事件（P4）：任一控件变更时触发当前 RepeatSpec
        public event EventHandler<RepeatSpec?>? RepeatSpecChanged;
        private bool _isApplying; // 回填 UI 时抑制事件
        private readonly System.Windows.Forms.Timer _debounceTimer = new() { Interval = 250 };

        public RepeatSettingsForm()
        {
            Text = "重复设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // 防抖定时器：空闲 250ms 后触发一次变更事件
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                RaiseChanged();
            };

            // 布局根：垂直堆叠的多个 GroupBox
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(12),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 1) 模式
            var gbMode = new GroupBox
                { Text = "模式", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10) };
            var pnlModes = new FlowLayoutPanel
                { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            pnlModes.Controls.AddRange([_rbNone, _rbDaily, _rbWeekly, _rbMonthly, _rbYearly, _rbCustom]);
            gbMode.Controls.Add(pnlModes);
            root.Controls.Add(gbMode, 0, 0);

            // 2) 自定义周期
            var gbCustom = new GroupBox
            {
                Text = "自定义周期（≥0，至少一项>0）", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10)
            };
            var tlCustom = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 12 };
            for (var i = 0; i < 12; i++) tlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlCustom.Controls.Add(_numYears, 0, 0);
            tlCustom.Controls.Add(MakeLabel("年"), 1, 0);
            tlCustom.Controls.Add(_numMonths, 2, 0);
            tlCustom.Controls.Add(MakeLabel("月"), 3, 0);
            tlCustom.Controls.Add(_numDays, 4, 0);
            tlCustom.Controls.Add(MakeLabel("日"), 5, 0);
            tlCustom.Controls.Add(_numHours, 6, 0);
            tlCustom.Controls.Add(MakeLabel("时"), 7, 0);
            tlCustom.Controls.Add(_numMinutes, 8, 0);
            tlCustom.Controls.Add(MakeLabel("分"), 9, 0);
            tlCustom.Controls.Add(_numSeconds, 10, 0);
            tlCustom.Controls.Add(MakeLabel("秒"), 11, 0);
            gbCustom.Controls.Add(tlCustom);
            root.Controls.Add(gbCustom, 0, 1);

            // 3) 结束时间
            var gbEnd = new GroupBox
            {
                Text = "结束时间（到此不再提醒，可选）", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10)
            };
            var tlEnd = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 9 };
            for (var i = 0; i < 9; i++) tlEnd.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlEnd.Controls.Add(MakeLabel("日期"), 0, 0);
            tlEnd.Controls.Add(_dtEndDate, 1, 0);
            tlEnd.Controls.Add(_endHour, 2, 0);
            tlEnd.Controls.Add(MakeLabel("时"), 3, 0);
            tlEnd.Controls.Add(_endMinute, 4, 0);
            tlEnd.Controls.Add(MakeLabel("分"), 5, 0);
            tlEnd.Controls.Add(_endSecond, 6, 0);
            tlEnd.Controls.Add(MakeLabel("秒"), 7, 0);
            tlEnd.Controls.Add(_chkNoEnd, 8, 0);
            gbEnd.Controls.Add(tlEnd);
            root.Controls.Add(gbEnd, 0, 2);

            // 4) 跳过规则
            var gbSkip = new GroupBox
            {
                Text = "跳过规则", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10)
            };
            var tlSkip = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 5 };
            for (var i = 0; i < 5; i++) tlSkip.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlSkip.Controls.Add(MakeLabel("每提醒"), 0, 0);
            tlSkip.Controls.Add(_numRemindTimes, 1, 0);
            tlSkip.Controls.Add(MakeLabel("次，跳过"), 2, 0);
            tlSkip.Controls.Add(_numSkipTimes, 3, 0);
            tlSkip.Controls.Add(MakeLabel("次"), 4, 0);
            gbSkip.Controls.Add(tlSkip);
            root.Controls.Add(gbSkip, 0, 3);

            // 4.5) 暂停规则
            var gbPause = new GroupBox
            {
                Text = "暂停规则", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10)
            };
            var tlPause = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            tlPause.Controls.Add(_chkPause);
            gbPause.Controls.Add(tlPause);
            root.Controls.Add(gbPause, 0, 4);

            // 5) 提示区（左对齐）：显示校验/提示信息
            var pnlHint = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0, 8, 0, 0)
            };
            _lblHint.ForeColor = SystemColors.GrayText;
            _lblHint.Text = string.Empty;
            pnlHint.Controls.Add(_lblHint);
            root.Controls.Add(pnlHint, 0, 5);

            // 6) 按钮区（右对齐）：确定 / 取消
            var pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0, 10, 0, 0)
            };
            var btnOk = new Button { Text = "确定", AutoSize = true };
            var btnCancel = new Button { Text = "取消", AutoSize = true };
            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Controls.Add(btnCancel);
            root.Controls.Add(pnlButtons, 0, 6);

            Controls.Add(root);

            // 默认值与事件
            _rbNone.Checked = true;
            var now = DateTime.Now;
            _dtEndDate.Value = now.Date;
            _endHour.Value = now.Hour;
            _endMinute.Value = now.Minute;
            _endSecond.Value = 0; // 秒默认0
            _chkNoEnd.Checked = true; // 默认“无”，表示始终重复

            // 切换启用状态
            foreach (var rb in new[] { _rbNone, _rbDaily, _rbWeekly, _rbMonthly, _rbYearly, _rbCustom })
            {
                rb.CheckedChanged += (_, _) =>
                {
                    UpdateEnabledState();
                    CueChange();
                };
            }

            _numYears.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };
            _numMonths.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };
            _numDays.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };
            _numHours.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };
            _numMinutes.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };
            _numSeconds.ValueChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };

            // 结束时间与跳过：值变更即触发
            _chkNoEnd.CheckedChanged += (_, _) =>
            {
                UpdateEnabledState();
                UpdateValidationMessage();
                CueChange();
            };
            _dtEndDate.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };
            _endHour.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };
            _endMinute.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };
            _endSecond.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };
            _numRemindTimes.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };
            _numSkipTimes.ValueChanged += (_, _) =>
            {
                UpdateValidationMessage();
                CueChange();
            };

            _chkPause.CheckedChanged += (_, _) =>
            {
                UpdateEnabledState();
                CueChange();
            };

            UpdateEnabledState();
            UpdateValidationMessage();

            // 事件：确定/取消
            btnOk.Click += (_, _) => DialogResult = DialogResult.OK;
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // 关闭前冲刷一次待保存（防止 X 关闭时 250ms 防抖未触发导致丢失）
            FormClosing += (_, _) =>
            {
                try
                {
                    if (_debounceTimer.Enabled) _debounceTimer.Stop();
                    RaiseChanged();
                }
                catch
                {
                    // 忽略
                }
            };

            // 关闭时停止并释放定时器
            FormClosed += (_, _) =>
            {
                try
                {
                    _debounceTimer.Stop();
                }
                catch
                {
                    // 忽略
                }

                try
                {
                    _debounceTimer.Dispose();
                }
                catch
                {
                    // 忽略
                }
            };
        }

        private static Label MakeLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 0, 6, 0)
        };

        private void UpdateEnabledState()
        {
            var custom = _rbCustom.Checked;
            var none = _rbNone.Checked;

            // 自定义周期启用/禁用
            foreach (var ctl in new Control[] { _numYears, _numMonths, _numDays, _numHours, _numMinutes, _numSeconds })
                ctl.Enabled = custom;

            // 是否认为“没有配置周期”：None 或（Custom 且全部为 0）
            var customAllZero = _numYears.Value == 0 && _numMonths.Value == 0 && _numDays.Value == 0 &&
                                 _numHours.Value == 0 && _numMinutes.Value == 0 && _numSeconds.Value == 0;
            var noPeriod = none || (custom && customAllZero);

            // 当无周期时禁用结束时间与跳过
            _chkNoEnd.Enabled = !noPeriod;
            var endEnabled = !noPeriod && !_chkNoEnd.Checked;
            foreach (var ctl in new Control[] { _dtEndDate, _endHour, _endMinute, _endSecond })
                ctl.Enabled = endEnabled;
            foreach (var ctl in new Control[] { _numRemindTimes, _numSkipTimes })
                ctl.Enabled = !noPeriod;

            // 暂停选项
            _chkPause.Enabled = !noPeriod;

            UpdateValidationMessage();
        }

        private RepeatSpec _BuildSpecFromUI()
        {
            var mode = _rbNone.Checked ? RepeatMode.None :
                _rbDaily.Checked ? RepeatMode.Daily :
                _rbWeekly.Checked ? RepeatMode.Weekly :
                _rbMonthly.Checked ? RepeatMode.Monthly :
                _rbYearly.Checked ? RepeatMode.Yearly :
                RepeatMode.Custom;

            if (mode == RepeatMode.None)
                return new RepeatSpec { Mode = RepeatMode.None };

            RepeatCustom? custom = null;
            if (mode == RepeatMode.Custom)
            {
                custom = new RepeatCustom
                {
                    Years = (int)_numYears.Value,
                    Months = (int)_numMonths.Value,
                    Days = (int)_numDays.Value,
                    Hours = (int)_numHours.Value,
                    Minutes = (int)_numMinutes.Value,
                    Seconds = (int)_numSeconds.Value
                };
                // 自定义周期至少一项>0，否则等价于不重复
                if (custom.IsEmpty)
                {
                    mode = RepeatMode.None;
                    custom = null;
                }
            }

            DateTime? endAt = null;
            if (_dtEndDate.Enabled && !_chkNoEnd.Checked)
            {
                var date = _dtEndDate.Value.Date;
                var h = (int)_endHour.Value;
                var m = (int)_endMinute.Value;
                var s = (int)_endSecond.Value;
                var candidate = date.AddHours(h).AddMinutes(m).AddSeconds(s);
                // 结束时间不可为过去：若为过去则忽略
                if (candidate > DateTime.Now)
                    endAt = candidate;
            }

            SkipRule? skip = null;
            if (_numRemindTimes is { Enabled: true, Value: > 0m } && _numSkipTimes is { Value: > 0m })
            {
                skip = new SkipRule
                {
                    RemindTimes = (int)_numRemindTimes.Value,
                    SkipTimes = (int)_numSkipTimes.Value
                };
            }

            return new RepeatSpec
            {
                Mode = mode,
                Custom = mode == RepeatMode.Custom ? custom : null,
                EndAt = endAt,
                Skip = skip,
                PauseUntilDone = _chkPause.Checked
            };
        }

        private void UpdateValidationMessage()
        {
            var (msg, isError) = GetValidationMessage();
            _lblHint.Text = msg;
            _lblHint.ForeColor = isError ? Color.Red : SystemColors.GrayText;
        }

        private (string message, bool isError) GetValidationMessage()
        {
            // 自定义周期校验
            if (_rbCustom.Checked)
            {
                var empty = _numYears.Value == 0 && _numMonths.Value == 0 && _numDays.Value == 0 &&
                            _numHours.Value == 0 && _numMinutes.Value == 0 && _numSeconds.Value == 0;
                if (empty)
                    return ("自定义周期至少一项 > 0", true);
            }

            // 结束时间校验（仅在启用时）
            if (_dtEndDate.Enabled && !_chkNoEnd.Checked)
            {
                var candidate = _dtEndDate.Value.Date
                    .AddHours((int)_endHour.Value)
                    .AddMinutes((int)_endMinute.Value)
                    .AddSeconds((int)_endSecond.Value);
                if (candidate <= DateTime.Now)
                    return ("结束时间不能早于当前时间（将被忽略）", true);
            }

            // 跳过规则提示（非错误）
            if (_numRemindTimes is { Enabled: true } && !(_numRemindTimes.Value > 0m && _numSkipTimes.Value > 0m))
                return ("提示：跳过规则需两者 > 0 才会生效", false);

            return (string.Empty, false);
        }

        private void _ApplySpecToUI(RepeatSpec? spec)
        {
            // 防止回填期间触发旧的防抖事件
            try
            {
                _debounceTimer.Stop();
            }
            catch
            {
                // 忽略
            }

            _isApplying = true;
            try
            {
                spec ??= new RepeatSpec { Mode = RepeatMode.None };
                _rbNone.Checked = spec.Mode == RepeatMode.None;
                _rbDaily.Checked = spec.Mode == RepeatMode.Daily;
                _rbWeekly.Checked = spec.Mode == RepeatMode.Weekly;
                _rbMonthly.Checked = spec.Mode == RepeatMode.Monthly;
                _rbYearly.Checked = spec.Mode == RepeatMode.Yearly;
                _rbCustom.Checked = spec.Mode == RepeatMode.Custom;

                var c = spec.Custom;
                _numYears.Value = c?.Years ?? 0;
                _numMonths.Value = c?.Months ?? 0;
                _numDays.Value = c?.Days ?? 0;
                _numHours.Value = c?.Hours ?? 0;
                _numMinutes.Value = c?.Minutes ?? 0;
                _numSeconds.Value = c?.Seconds ?? 0;

                if (spec.EndAt.HasValue)
                {
                    var e = spec.EndAt.Value;
                    _dtEndDate.Value = e.Date;
                    _endHour.Value = e.Hour;
                    _endMinute.Value = e.Minute;
                    _endSecond.Value = e.Second;
                    _chkNoEnd.Checked = false;
                }
                else
                {
                    var now = DateTime.Now;
                    _dtEndDate.Value = now.Date;
                    _endHour.Value = now.Hour;
                    _endMinute.Value = now.Minute;
                    _endSecond.Value = 0;
                    _chkNoEnd.Checked = true;
                }

                var s = spec.Skip;
                _numRemindTimes.Value = s?.RemindTimes > 0 ? s.RemindTimes : 0;
                _numSkipTimes.Value = s?.SkipTimes > 0 ? s.SkipTimes : 0;

                _chkPause.Checked = spec.PauseUntilDone;

                UpdateEnabledState();
            }
            finally
            {
                _isApplying = false;
            }
            // 回填完成后不主动触发事件（避免外部循环），等待用户进一步更改
        }

        private void RaiseChanged()
        {
            if (_isApplying) return;
            RepeatSpecChanged?.Invoke(this, _BuildSpecFromUI());
        }

        private void CueChange()
        {
            if (_isApplying) return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }
}