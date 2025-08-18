/*
 * 游戏升级提醒 - 提前通知时间设置对话框
 * 作者: YuanXiQWQ
 * 描述: 允许用户输入天/时/分/秒来设置提前通知时间
 */


namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// 自定义“提前通知时间”的模态对话框。
    /// 用户通过输入“天/时/分/秒”，点击“确定”后从 <see cref="TotalSeconds"/> 读取总秒数。
    /// 典型用法：
    /// <code>
    /// using (var dlg = new AdvanceTimeDialog(currentAdvance))
    ///     if (dlg.ShowDialog(owner) == DialogResult.OK)
    ///     {
    ///         int secs = dlg.TotalSeconds;
    ///         // 持久化或应用到设置
    ///     }
    /// </code>
    /// </summary>
    internal sealed class AdvanceTimeDialog : Form
    {
        private readonly NumericUpDown numDays = new() { Minimum = 0, Maximum = 3650, Width = 60 };
        private readonly NumericUpDown numHours = new() { Minimum = 0, Maximum = 999, Width = 60 };
        private readonly NumericUpDown numMinutes = new() { Minimum = 0, Maximum = 59, Width = 60 };
        private readonly NumericUpDown numSeconds = new() { Minimum = 0, Maximum = 59, Width = 60 };

        /// <summary>
        /// 汇总得到的总秒数（单位：秒）。
        /// 仅在用户点击“确定”时更新；点击“取消”不更新。
        /// </summary>
        public int TotalSeconds { get; private set; }

        /// <summary>
        /// 创建对话框，并使用给定的初始秒数预填各输入框。
        /// </summary>
        /// <param name="initialSeconds">初始秒数；小于 0 时按 0 处理。</param>
        public AdvanceTimeDialog(int initialSeconds = 0)
        {
            Text = "自定义提前通知";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // 布局
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 8,
                Padding = new Padding(12),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void AddLabel(string text, int col)
            {
                var lb = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 6, 6, 0) };
                root.Controls.Add(lb, col, 0);
            }

            // 第 0 行（输入区）：天/时/分/秒
            root.Controls.Add(numDays, 0, 0);
            AddLabel("天", 1);
            root.Controls.Add(numHours, 2, 0);
            AddLabel("小时", 3);
            root.Controls.Add(numMinutes, 4, 0);
            AddLabel("分钟", 5);
            root.Controls.Add(numSeconds, 6, 0);
            AddLabel("秒", 7);

            // 按钮区（右对齐）：确定 / 取消
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
            root.SetColumnSpan(pnlButtons, 8);
            root.Controls.Add(pnlButtons, 0, 1);

            Controls.Add(root);

            // 事件：点击“确定”时计算总秒数并设置对话结果
            btnOk.Click += (_, _) =>
            {
                TotalSeconds = CalcTotalSeconds();
                DialogResult = DialogResult.OK;
            };
            // 事件：点击“取消”直接关闭，不修改 TotalSeconds
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // 根据 initialSeconds 初始化（将秒拆分为天/时/分/秒）
            if (initialSeconds < 0) initialSeconds = 0;
            var ts = TimeSpan.FromSeconds(initialSeconds);
            numDays.Value = ts.Days;
            // 若总小时超过控件上限，则裁剪到上限（避免 UI 上的越界）
            numHours.Value = ts.Hours + ts.Days * 24 > numHours.Maximum ? numHours.Maximum : ts.Hours;
            numMinutes.Value = ts.Minutes;
            numSeconds.Value = ts.Seconds;
        }

        /// <summary>
        /// 将四个输入框的值汇总为总秒数。
        /// 使用 <see langword="checked"/> 防止整型溢出；如发生异常，返回 <see cref="int.MaxValue"/>。
        /// 调用方通常不应将该极值视为有效输入。
        /// </summary>
        private int CalcTotalSeconds()
        {
            try
            {
                var days = (int)numDays.Value;
                var hours = (int)numHours.Value;
                var minutes = (int)numMinutes.Value;
                var seconds = (int)numSeconds.Value;
                var total = checked(days * 24 * 3600 + hours * 3600 + minutes * 60 + seconds);
                if (total < 0) total = 0;
                return total;
            }
            catch
            {
                return int.MaxValue;
            }
        }
    }
}
