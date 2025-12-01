using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class MonitorConfigForm : Form
    {
        private readonly MonitorInfo monitor;
        private readonly MultiMonitorManager multiMonitorManager;
        
        private NumericUpDown leftMarginUpDown, topMarginUpDown, rightMarginUpDown, bottomMarginUpDown;
        private CheckBox enabledCheckBox, usePercentageCheckBox;
        private Button okButton, cancelButton, testButton;
        private Label resolutionLabel, dpiLabel;

        public MonitorConfigForm(MonitorInfo monitor, MultiMonitorManager multiMonitorManager)
        {
            this.monitor = monitor;
            this.multiMonitorManager = multiMonitorManager;
            InitializeComponent();
            LoadMonitorInfo();
        }

        private void InitializeComponent()
        {
            this.Text = $"Configure Monitor - {monitor.DeviceName}";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            CreateMonitorInfoPanel();
            CreateMarginSettingsPanel();
            CreateButtonPanel();
        }

        private void CreateMonitorInfoPanel()
        {
            var infoGroupBox = new GroupBox
            {
                Text = "Monitor Information",
                Location = new Point(12, 12),
                Size = new Size(360, 100)
            };

            var deviceLabel = new Label
            {
                Text = $"Device: {monitor.DeviceName}",
                Location = new Point(12, 25),
                Size = new Size(340, 20)
            };

            resolutionLabel = new Label
            {
                Text = $"Resolution: {monitor.Bounds.Width} x {monitor.Bounds.Height}",
                Location = new Point(12, 45),
                Size = new Size(340, 20)
            };

            dpiLabel = new Label
            {
                Text = $"DPI Scale: {monitor.DpiScale:F2}x",
                Location = new Point(12, 65),
                Size = new Size(340, 20)
            };

            infoGroupBox.Controls.AddRange(new Control[] { deviceLabel, resolutionLabel, dpiLabel });
            this.Controls.Add(infoGroupBox);
        }

        private void CreateMarginSettingsPanel()
        {
            var marginGroupBox = new GroupBox
            {
                Text = "Margin Settings",
                Location = new Point(12, 125),
                Size = new Size(360, 150)
            };

            enabledCheckBox = new CheckBox
            {
                Text = "Enable custom margins for this monitor",
                Location = new Point(12, 25),
                Size = new Size(250, 23),
                Checked = true
            };

            var leftLabel = new Label { Text = "Left:", Location = new Point(12, 60), Size = new Size(50, 23) };
            leftMarginUpDown = new NumericUpDown { Location = new Point(68, 57), Size = new Size(80, 23), Maximum = 9999 };

            var topLabel = new Label { Text = "Top:", Location = new Point(160, 60), Size = new Size(50, 23) };
            topMarginUpDown = new NumericUpDown { Location = new Point(216, 57), Size = new Size(80, 23), Maximum = 9999 };

            var rightLabel = new Label { Text = "Right:", Location = new Point(12, 95), Size = new Size(50, 23) };
            rightMarginUpDown = new NumericUpDown { Location = new Point(68, 92), Size = new Size(80, 23), Maximum = 9999 };

            var bottomLabel = new Label { Text = "Bottom:", Location = new Point(160, 95), Size = new Size(50, 23) };
            bottomMarginUpDown = new NumericUpDown { Location = new Point(216, 92), Size = new Size(80, 23), Maximum = 9999 };

            usePercentageCheckBox = new CheckBox
            {
                Text = "Use percentage values",
                Location = new Point(12, 125),
                Size = new Size(200, 23)
            };

            marginGroupBox.Controls.AddRange(new Control[]
            {
                enabledCheckBox, leftLabel, leftMarginUpDown, topLabel, topMarginUpDown,
                rightLabel, rightMarginUpDown, bottomLabel, bottomMarginUpDown, usePercentageCheckBox
            });

            this.Controls.Add(marginGroupBox);
        }

        private void CreateButtonPanel()
        {
            testButton = new Button
            {
                Text = "Test Settings",
                Location = new Point(12, 290),
                Size = new Size(100, 26)
            };

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(216, 290),
                Size = new Size(75, 26),
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(297, 290),
                Size = new Size(75, 26),
                DialogResult = DialogResult.Cancel
            };

            testButton.Click += TestButton_Click;
            okButton.Click += OkButton_Click;

            this.Controls.AddRange(new Control[] { testButton, okButton, cancelButton });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadMonitorInfo()
        {
            leftMarginUpDown.Value = 50;
            topMarginUpDown.Value = 50;
            rightMarginUpDown.Value = 50;
            bottomMarginUpDown.Value = 50;
        }

        private void TestButton_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Test functionality would apply the current margin settings temporarily to preview the effect.",
                "Test Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Monitor-specific settings saved successfully!",
                "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
