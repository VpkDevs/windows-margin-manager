using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class SettingsForm : Form
    {
        private readonly MarginSettings settings;
        private NumericUpDown leftMarginInput;
        private NumericUpDown topMarginInput;
        private NumericUpDown rightMarginInput;
        private NumericUpDown bottomMarginInput;
        private RadioButton pixelsRadio;
        private RadioButton percentageRadio;
        private Button okButton;
        private Button cancelButton;
        private Button applyButton;

        public SettingsForm(MarginSettings settings)
        {
            this.settings = settings;
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Margin Settings";
            this.Size = new Size(350, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var marginGroupBox = new GroupBox
            {
                Text = "Margins",
                Location = new Point(12, 12),
                Size = new Size(310, 140)
            };

            var leftLabel = new Label
            {
                Text = "Left:",
                Location = new Point(15, 25),
                Size = new Size(40, 23)
            };

            leftMarginInput = new NumericUpDown
            {
                Location = new Point(60, 23),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 1000,
                Value = settings.LeftMargin
            };

            var topLabel = new Label
            {
                Text = "Top:",
                Location = new Point(140, 25),
                Size = new Size(40, 23)
            };

            topMarginInput = new NumericUpDown
            {
                Location = new Point(180, 23),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 1000,
                Value = settings.TopMargin
            };

            var rightLabel = new Label
            {
                Text = "Right:",
                Location = new Point(15, 55),
                Size = new Size(40, 23)
            };

            rightMarginInput = new NumericUpDown
            {
                Location = new Point(60, 53),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 1000,
                Value = settings.RightMargin
            };

            var bottomLabel = new Label
            {
                Text = "Bottom:",
                Location = new Point(140, 55),
                Size = new Size(50, 23)
            };

            bottomMarginInput = new NumericUpDown
            {
                Location = new Point(190, 53),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 1000,
                Value = settings.BottomMargin
            };

            pixelsRadio = new RadioButton
            {
                Text = "Pixels",
                Location = new Point(15, 85),
                Size = new Size(80, 23),
                Checked = !settings.UsePercentage
            };

            percentageRadio = new RadioButton
            {
                Text = "Percentage",
                Location = new Point(100, 85),
                Size = new Size(100, 23),
                Checked = settings.UsePercentage
            };

            var uniformButton = new Button
            {
                Text = "Set All",
                Location = new Point(220, 110),
                Size = new Size(70, 23)
            };
            uniformButton.Click += OnSetUniform;

            marginGroupBox.Controls.AddRange(new Control[]
            {
                leftLabel, leftMarginInput, topLabel, topMarginInput,
                rightLabel, rightMarginInput, bottomLabel, bottomMarginInput,
                pixelsRadio, percentageRadio, uniformButton
            });

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(90, 210),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OnOK;

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(175, 210),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            applyButton = new Button
            {
                Text = "Apply",
                Location = new Point(260, 210),
                Size = new Size(75, 23)
            };
            applyButton.Click += OnApply;

            this.Controls.AddRange(new Control[]
            {
                marginGroupBox, okButton, cancelButton, applyButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadCurrentSettings()
        {
            leftMarginInput.Value = settings.LeftMargin;
            topMarginInput.Value = settings.TopMargin;
            rightMarginInput.Value = settings.RightMargin;
            bottomMarginInput.Value = settings.BottomMargin;
            pixelsRadio.Checked = !settings.UsePercentage;
            percentageRadio.Checked = settings.UsePercentage;
        }

        private void OnSetUniform(object? sender, EventArgs e)
        {
            var uniformForm = new UniformMarginForm();
            if (uniformForm.ShowDialog() == DialogResult.OK)
            {
                var value = uniformForm.MarginValue;
                leftMarginInput.Value = value;
                topMarginInput.Value = value;
                rightMarginInput.Value = value;
                bottomMarginInput.Value = value;
            }
        }

        private void OnApply(object? sender, EventArgs e)
        {
            ApplySettings();
        }

        private void OnOK(object? sender, EventArgs e)
        {
            ApplySettings();
            this.Close();
        }

        private void ApplySettings()
        {
            settings.LeftMargin = (int)leftMarginInput.Value;
            settings.TopMargin = (int)topMarginInput.Value;
            settings.RightMargin = (int)rightMarginInput.Value;
            settings.BottomMargin = (int)bottomMarginInput.Value;
            settings.UsePercentage = percentageRadio.Checked;
            settings.SaveSettings();
        }
    }

    public partial class UniformMarginForm : Form
    {
        private NumericUpDown marginInput;
        public int MarginValue { get; private set; }

        public UniformMarginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Set Uniform Margin";
            this.Size = new Size(250, 120);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                Text = "Margin value:",
                Location = new Point(12, 15),
                Size = new Size(80, 23)
            };

            marginInput = new NumericUpDown
            {
                Location = new Point(100, 13),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 1000,
                Value = 50
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(70, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };
            okButton.Click += (s, e) => { MarginValue = (int)marginInput.Value; };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(155, 50),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { label, marginInput, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
