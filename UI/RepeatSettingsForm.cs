/*
 * 游戏升级提醒 - 重复设置窗口（P3+P4+P9：UI、变更即保存与校验）
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
        private readonly RadioButton rbNone = new() { Text = "不重复", AutoSize = true };
        private readonly RadioButton rbDaily = new() { Text = "每天", AutoSize = true };
        private readonly RadioButton rbWeekly = new() { Text = "每周", AutoSize = true };
        private readonly RadioButton rbMonthly = new() { Text = "每月", AutoSize = true };
        private readonly RadioButton rbYearly = new() { Text = "每年", AutoSize = true };
        private readonly RadioButton rbCustom = new() { Text = "自定义", AutoSize = true };

        // 自定义周期
        private readonly NumericUpDown numYears = new() { Minimum = 0, Maximum = 1000, Width = 60 };
        private readonly NumericUpDown numMonths = new() { Minimum = 0, Maximum = 1200, Width = 60 };
        private readonly NumericUpDown numDays = new() { Minimum = 0, Maximum = 365000, Width = 60 };
        private readonly NumericUpDown numHours = new() { Minimum = 0, Maximum = 100000, Width = 60 };
        private readonly NumericUpDown numMinutes = new() { Minimum = 0, Maximum = 100000, Width = 60 };
        private readonly NumericUpDown numSeconds = new() { Minimum = 0, Maximum = 100000, Width = 60 };

        // 结束时间
        private readonly DateTimePicker dtEndDate = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            ShowUpDown = false,
            Width = 120
        };
        private readonly NumericUpDown endHour = new() { Minimum = 0, Maximum = 23, Width = 60 };
        private readonly NumericUpDown endMinute = new() { Minimum = 0, Maximum = 59, Width = 60 };
        private readonly NumericUpDown endSecond = new() { Minimum = 0, Maximum = 59, Width = 60 };

        // 结束时间可选：无（默认）
        private readonly CheckBox chkNoEnd = new() { Text = "无", AutoSize = true };

        // 跳过规则
        private readonly NumericUpDown numRemindTimes = new() { Minimum = 0, Maximum = 100000, Width = 80 };
        private readonly NumericUpDown numSkipTimes = new() { Minimum = 0, Maximum = 100000, Width = 80 };

        // 校验与提示
        private readonly Label lblHint = new() { AutoSize = true };

        /// <summary>
        /// 当前编辑的重复设置。读取时根据 UI 组合，设置时回填 UI。
        /// null 或 Mode=None 视为“不重复”。
        /// </summary>
        public RepeatSpec? CurrentSpec
        {
            get => BuildSpecFromUI();
            set => ApplySpecToUI(value);
        }

        // 变更即保存事件（P4）：任一控件变更时触发当前 RepeatSpec
        public event EventHandler<RepeatSpec?>? RepeatSpecChanged;
        private bool isApplying; // 回填 UI 时抑制事件
        private readonly System.Windows.Forms.Timer debounceTimer = new() { Interval = 250 };

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
            debounceTimer.Tick += (_, _) =>
            {
                debounceTimer.Stop();
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
            var gbMode = new GroupBox { Text = "模式", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10) };
            var pnlModes = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            pnlModes.Controls.AddRange([ rbNone, rbDaily, rbWeekly, rbMonthly, rbYearly, rbCustom ]);
            gbMode.Controls.Add(pnlModes);
            root.Controls.Add(gbMode, 0, 0);

            // 2) 自定义周期
            var gbCustom = new GroupBox { Text = "自定义周期（≥0，至少一项>0）", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10) };
            var tlCustom = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 12 };
            for (int i = 0; i < 12; i++) tlCustom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlCustom.Controls.Add(numYears, 0, 0); tlCustom.Controls.Add(MakeLabel("年"), 1, 0);
            tlCustom.Controls.Add(numMonths, 2, 0); tlCustom.Controls.Add(MakeLabel("月"), 3, 0);
            tlCustom.Controls.Add(numDays, 4, 0); tlCustom.Controls.Add(MakeLabel("日"), 5, 0);
            tlCustom.Controls.Add(numHours, 6, 0); tlCustom.Controls.Add(MakeLabel("时"), 7, 0);
            tlCustom.Controls.Add(numMinutes, 8, 0); tlCustom.Controls.Add(MakeLabel("分"), 9, 0);
            tlCustom.Controls.Add(numSeconds, 10, 0); tlCustom.Controls.Add(MakeLabel("秒"), 11, 0);
            gbCustom.Controls.Add(tlCustom);
            root.Controls.Add(gbCustom, 0, 1);

            // 3) 结束时间
            var gbEnd = new GroupBox { Text = "结束时间（到此不再提醒，可选）", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10) };
            var tlEnd = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 9 };
            for (int i = 0; i < 9; i++) tlEnd.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlEnd.Controls.Add(MakeLabel("日期"), 0, 0);
            tlEnd.Controls.Add(dtEndDate, 1, 0);
            tlEnd.Controls.Add(MakeLabel("时"), 2, 0);
            tlEnd.Controls.Add(endHour, 3, 0);
            tlEnd.Controls.Add(MakeLabel("分"), 4, 0);
            tlEnd.Controls.Add(endMinute, 5, 0);
            tlEnd.Controls.Add(MakeLabel("秒"), 6, 0);
            tlEnd.Controls.Add(endSecond, 7, 0);
            tlEnd.Controls.Add(chkNoEnd, 8, 0);
            gbEnd.Controls.Add(tlEnd);
            root.Controls.Add(gbEnd, 0, 2);

            // 4) 跳过规则
            var gbSkip = new GroupBox { Text = "跳过规则（每提醒A次，跳过B次）", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(10, 8, 10, 10) };
            var tlSkip = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 6 };
            for (int i = 0; i < 6; i++) tlSkip.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlSkip.Controls.Add(MakeLabel("A:"), 0, 0);
            tlSkip.Controls.Add(numRemindTimes, 1, 0);
            tlSkip.Controls.Add(MakeLabel("次，跳过 B:"), 2, 0);
            tlSkip.Controls.Add(numSkipTimes, 3, 0);
            tlSkip.Controls.Add(MakeLabel("次"), 4, 0);
            gbSkip.Controls.Add(tlSkip);
            root.Controls.Add(gbSkip, 0, 3);

            // 5) 提示区（左对齐）：显示校验/提示信息
            var pnlHint = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0, 8, 0, 0)
            };
            lblHint.ForeColor = System.Drawing.SystemColors.GrayText;
            lblHint.Text = string.Empty;
            pnlHint.Controls.Add(lblHint);
            root.Controls.Add(pnlHint, 0, 4);

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
            root.Controls.Add(pnlButtons, 0, 5);

            Controls.Add(root);

            // 默认值与事件
            rbNone.Checked = true;
            var now = DateTime.Now;
            dtEndDate.Value = now.Date;
            endHour.Value = now.Hour;
            endMinute.Value = now.Minute;
            endSecond.Value = 0; // 秒默认0
            chkNoEnd.Checked = true; // 默认“无”，表示始终重复

            // 切换启用状态
            foreach (var rb in new[] { rbNone, rbDaily, rbWeekly, rbMonthly, rbYearly, rbCustom })
            {
                rb.CheckedChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            }
            numYears.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            numMonths.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            numDays.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            numHours.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            numMinutes.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };
            numSeconds.ValueChanged += (_, _) => { UpdateEnabledState(); CueChange(); };

            // 结束时间与跳过：值变更即触发
            chkNoEnd.CheckedChanged += (_, _) => { UpdateEnabledState(); UpdateValidationMessage(); CueChange(); };
            dtEndDate.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };
            endHour.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };
            endMinute.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };
            endSecond.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };
            numRemindTimes.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };
            numSkipTimes.ValueChanged += (_, _) => { UpdateValidationMessage(); CueChange(); };

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
                    if (debounceTimer.Enabled) debounceTimer.Stop();
                    RaiseChanged();
                }
                catch { }
            };

            // 关闭时停止并释放定时器
            FormClosed += (_, _) =>
            {
                try { debounceTimer.Stop(); } catch { }
                try { debounceTimer.Dispose(); } catch { }
            };
        }

        private static Label MakeLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(6, 6, 6, 0)
        };

        private void UpdateEnabledState()
        {
            bool custom = rbCustom.Checked;
            bool none = rbNone.Checked;

            // 自定义周期启用/禁用
            foreach (var ctl in new Control[] { numYears, numMonths, numDays, numHours, numMinutes, numSeconds })
                ctl.Enabled = custom;

            // 是否认为“没有配置周期”：None 或（Custom 且全部为 0）
            bool customAllZero = numYears.Value == 0 && numMonths.Value == 0 && numDays.Value == 0 &&
                                 numHours.Value == 0 && numMinutes.Value == 0 && numSeconds.Value == 0;
            bool noPeriod = none || (custom && customAllZero);

            // 当无周期时禁用结束时间与跳过
            chkNoEnd.Enabled = !noPeriod;
            var endEnabled = !noPeriod && !chkNoEnd.Checked;
            foreach (var ctl in new Control[] { dtEndDate, endHour, endMinute, endSecond })
                ctl.Enabled = endEnabled;
            foreach (var ctl in new Control[] { numRemindTimes, numSkipTimes })
                ctl.Enabled = !noPeriod;

            UpdateValidationMessage();
        }

        private RepeatSpec? BuildSpecFromUI()
        {
            var mode = rbNone.Checked ? RepeatMode.None :
                       rbDaily.Checked ? RepeatMode.Daily :
                       rbWeekly.Checked ? RepeatMode.Weekly :
                       rbMonthly.Checked ? RepeatMode.Monthly :
                       rbYearly.Checked ? RepeatMode.Yearly :
                       RepeatMode.Custom;

            if (mode == RepeatMode.None)
                return new RepeatSpec { Mode = RepeatMode.None };

            RepeatCustom? custom = null;
            if (mode == RepeatMode.Custom)
            {
                custom = new RepeatCustom
                {
                    Years = (int)numYears.Value,
                    Months = (int)numMonths.Value,
                    Days = (int)numDays.Value,
                    Hours = (int)numHours.Value,
                    Minutes = (int)numMinutes.Value,
                    Seconds = (int)numSeconds.Value
                };
                // 自定义周期至少一项>0，否则等价于不重复
                if (custom.IsEmpty)
                {
                    mode = RepeatMode.None;
                    custom = null;
                }
            }

            DateTime? endAt = null;
            if (dtEndDate.Enabled && !chkNoEnd.Checked)
            {
                var date = dtEndDate.Value.Date;
                var h = (int)endHour.Value;
                var m = (int)endMinute.Value;
                var s = (int)endSecond.Value;
                var candidate = date.AddHours(h).AddMinutes(m).AddSeconds(s);
                // 结束时间不可为过去：若为过去则忽略
                if (candidate > DateTime.Now)
                    endAt = candidate;
            }

            SkipRule? skip = null;
            if (numRemindTimes.Enabled)
            {
                var a = (int)numRemindTimes.Value;
                var b = (int)numSkipTimes.Value;
                if (a > 0 && b > 0) skip = new SkipRule { RemindTimes = a, SkipTimes = b };
            }

            return new RepeatSpec
            {
                Mode = mode,
                Custom = mode == RepeatMode.Custom ? custom : null,
                EndAt = endAt,
                Skip = skip
            };
        }

        private void UpdateValidationMessage()
        {
            var (msg, isError) = GetValidationMessage();
            lblHint.Text = msg;
            lblHint.ForeColor = isError ? System.Drawing.Color.Red : System.Drawing.SystemColors.GrayText;
        }

        private (string message, bool isError) GetValidationMessage()
        {
            // 自定义周期校验
            if (rbCustom.Checked)
            {
                var empty = numYears.Value == 0 && numMonths.Value == 0 && numDays.Value == 0 &&
                            numHours.Value == 0 && numMinutes.Value == 0 && numSeconds.Value == 0;
                if (empty)
                    return ("自定义周期至少一项 > 0", true);
            }

            // 结束时间校验（仅在启用时）
            if (dtEndDate.Enabled && !chkNoEnd.Checked)
            {
                var candidate = dtEndDate.Value.Date
                    .AddHours((int)endHour.Value)
                    .AddMinutes((int)endMinute.Value)
                    .AddSeconds((int)endSecond.Value);
                if (candidate <= DateTime.Now)
                    return ("结束时间不能早于当前时间（将被忽略）", true);
            }

            // 跳过规则提示（非错误）
            if (numRemindTimes.Enabled)
            {
                if (numRemindTimes.Value == 0 || numSkipTimes.Value == 0)
                    return ("提示：跳过规则需 A>0 且 B>0 才会生效", false);
            }

            return (string.Empty, false);
        }

        private void ApplySpecToUI(RepeatSpec? spec)
        {
            // 防止回填期间触发旧的防抖事件
            try { debounceTimer.Stop(); } catch { }
            isApplying = true;
            try
            {
                spec ??= new RepeatSpec { Mode = RepeatMode.None };
            rbNone.Checked = spec.Mode == RepeatMode.None;
            rbDaily.Checked = spec.Mode == RepeatMode.Daily;
            rbWeekly.Checked = spec.Mode == RepeatMode.Weekly;
            rbMonthly.Checked = spec.Mode == RepeatMode.Monthly;
            rbYearly.Checked = spec.Mode == RepeatMode.Yearly;
            rbCustom.Checked = spec.Mode == RepeatMode.Custom;

            var c = spec.Custom;
            numYears.Value = c?.Years ?? 0;
            numMonths.Value = c?.Months ?? 0;
            numDays.Value = c?.Days ?? 0;
            numHours.Value = c?.Hours ?? 0;
            numMinutes.Value = c?.Minutes ?? 0;
            numSeconds.Value = c?.Seconds ?? 0;

            if (spec.EndAt.HasValue)
            {
                var e = spec.EndAt.Value;
                dtEndDate.Value = e.Date;
                endHour.Value = e.Hour;
                endMinute.Value = e.Minute;
                endSecond.Value = e.Second;
                chkNoEnd.Checked = false;
            }
            else
            {
                var now = DateTime.Now;
                dtEndDate.Value = now.Date;
                endHour.Value = now.Hour;
                endMinute.Value = now.Minute;
                endSecond.Value = 0;
                chkNoEnd.Checked = true;
            }

            var s = spec.Skip;
            numRemindTimes.Value = s?.RemindTimes > 0 ? s.RemindTimes : 0;
            numSkipTimes.Value = s?.SkipTimes > 0 ? s.SkipTimes : 0;

            UpdateEnabledState();
            }
            finally
            {
                isApplying = false;
            }
            // 回填完成后不主动触发事件（避免外部循环），等待用户进一步更改
        }

        private void RaiseChanged()
        {
            if (isApplying) return;
            RepeatSpecChanged?.Invoke(this, BuildSpecFromUI());
        }

        private void CueChange()
        {
            if (isApplying) return;
            debounceTimer.Stop();
            debounceTimer.Start();
        }
    }
}
