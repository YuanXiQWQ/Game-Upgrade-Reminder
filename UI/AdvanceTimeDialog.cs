/*
 * 游戏升级提醒 - 提前通知时间设置对话框
 * 作者: YuanXiQWQ
 * 描述: 允许用户输入天/时/分/秒来设置提前通知时间
 */


namespace Game_Upgrade_Reminder.UI
{
    internal sealed class AdvanceTimeDialog : Form
    {
        private readonly NumericUpDown numDays = new() { Minimum = 0, Maximum = 3650, Width = 60 };
        private readonly NumericUpDown numHours = new() { Minimum = 0, Maximum = 999, Width = 60 };
        private readonly NumericUpDown numMinutes = new() { Minimum = 0, Maximum = 59, Width = 60 };
        private readonly NumericUpDown numSeconds = new() { Minimum = 0, Maximum = 59, Width = 60 };

        public int TotalSeconds { get; private set; }

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

            // Layout
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

            // Row 0
            root.Controls.Add(numDays, 0, 0);
            AddLabel("天", 1);
            root.Controls.Add(numHours, 2, 0);
            AddLabel("小时", 3);
            root.Controls.Add(numMinutes, 4, 0);
            AddLabel("分钟", 5);
            root.Controls.Add(numSeconds, 6, 0);
            AddLabel("秒", 7);

            // Buttons
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

            // Events
            btnOk.Click += (_, _) =>
            {
                TotalSeconds = CalcTotalSeconds();
                DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // Initialize from initialSeconds
            if (initialSeconds < 0) initialSeconds = 0;
            var ts = TimeSpan.FromSeconds(initialSeconds);
            numDays.Value = ts.Days;
            numHours.Value = ts.Hours + ts.Days * 24 > numHours.Maximum ? numHours.Maximum : ts.Hours;
            numMinutes.Value = ts.Minutes;
            numSeconds.Value = ts.Seconds;
        }

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
                return int.MaxValue; // very large if overflow
            }
        }
    }
}
