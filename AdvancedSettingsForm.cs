using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class AdvancedSettingsForm : Form
    {
        public MarginSettings MarginSettings { get; private set; }
        public WindowFilterCriteria WindowFilter { get; private set; }

        private readonly ProfileManager profileManager;
        private TabControl tabControl;
        private NumericUpDown leftMarginUpDown, topMarginUpDown, rightMarginUpDown, bottomMarginUpDown;
        private CheckBox usePercentageCheckBox, enableAnimationsCheckBox;
        private NumericUpDown animationDurationUpDown;
        private ComboBox easingComboBox, marginTypeComboBox;
        private TextBox processNamesTextBox, excludedProcessesTextBox;
        private CheckBox primaryMonitorOnlyCheckBox;
        private Button okButton, cancelButton, resetButton;

        public AdvancedSettingsForm(MarginSettings marginSettings, WindowFilterCriteria windowFilter, ProfileManager profileManager)
        {
            this.MarginSettings = new MarginSettings(marginSettings);
            this.WindowFilter = new WindowFilterCriteria
            {
                ProcessNames = windowFilter.ProcessNames?.ToList(),
                ExcludedProcesses = windowFilter.ExcludedProcesses?.ToList(),
                PrimaryMonitorOnly = windowFilter.PrimaryMonitorOnly
            };
            this.profileManager = profileManager;
            
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Advanced Settings";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(12)
            };

            CreateMarginSettingsTab();
            CreateAnimationSettingsTab();
            CreateFilterSettingsTab();
            CreateButtonPanel();

            this.Controls.Add(tabControl);
        }

        private void CreateMarginSettingsTab()
        {
            var marginTab = new TabPage("Margins");
            
            var marginTypeLabel = new Label
            {
                Text = "Margin Type:",
                Location = new Point(12, 15),
                Size = new Size(100, 23)
            };

            marginTypeComboBox = new ComboBox
            {
                Location = new Point(118, 12),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            marginTypeComboBox.Items.AddRange(Enum.GetNames(typeof(MarginType)));

            var leftLabel = new Label { Text = "Left:", Location = new Point(12, 50), Size = new Size(50, 23) };
            leftMarginUpDown = new NumericUpDown { Location = new Point(68, 47), Size = new Size(80, 23), Maximum = 9999 };

            var topLabel = new Label { Text = "Top:", Location = new Point(160, 50), Size = new Size(50, 23) };
            topMarginUpDown = new NumericUpDown { Location = new Point(216, 47), Size = new Size(80, 23), Maximum = 9999 };

            var rightLabel = new Label { Text = "Right:", Location = new Point(12, 85), Size = new Size(50, 23) };
            rightMarginUpDown = new NumericUpDown { Location = new Point(68, 82), Size = new Size(80, 23), Maximum = 9999 };

            var bottomLabel = new Label { Text = "Bottom:", Location = new Point(160, 85), Size = new Size(50, 23) };
            bottomMarginUpDown = new NumericUpDown { Location = new Point(216, 82), Size = new Size(80, 23), Maximum = 9999 };

            usePercentageCheckBox = new CheckBox
            {
                Text = "Use percentage values",
                Location = new Point(12, 120),
                Size = new Size(200, 23)
            };

            marginTab.Controls.AddRange(new Control[]
            {
                marginTypeLabel, marginTypeComboBox,
                leftLabel, leftMarginUpDown, topLabel, topMarginUpDown,
                rightLabel, rightMarginUpDown, bottomLabel, bottomMarginUpDown,
                usePercentageCheckBox
            });

            tabControl.TabPages.Add(marginTab);
        }

        private void CreateAnimationSettingsTab()
        {
            var animationTab = new TabPage("Animations");

            enableAnimationsCheckBox = new CheckBox
            {
                Text = "Enable animations",
                Location = new Point(12, 15),
                Size = new Size(200, 23)
            };

            var durationLabel = new Label
            {
                Text = "Duration (ms):",
                Location = new Point(12, 50),
                Size = new Size(100, 23)
            };

            animationDurationUpDown = new NumericUpDown
            {
                Location = new Point(118, 47),
                Size = new Size(100, 23),
                Minimum = 50,
                Maximum = 5000,
                Increment = 50
            };

            var easingLabel = new Label
            {
                Text = "Easing:",
                Location = new Point(12, 85),
                Size = new Size(100, 23)
            };

            easingComboBox = new ComboBox
            {
                Location = new Point(118, 82),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            easingComboBox.Items.AddRange(Enum.GetNames(typeof(EasingType)));

            animationTab.Controls.AddRange(new Control[]
            {
                enableAnimationsCheckBox, durationLabel, animationDurationUpDown,
                easingLabel, easingComboBox
            });

            tabControl.TabPages.Add(animationTab);
        }

        private void CreateFilterSettingsTab()
        {
            var filterTab = new TabPage("Window Filters");

            var processLabel = new Label
            {
                Text = "Include Processes (comma-separated):",
                Location = new Point(12, 15),
                Size = new Size(250, 23)
            };

            processNamesTextBox = new TextBox
            {
                Location = new Point(12, 41),
                Size = new Size(450, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var excludeLabel = new Label
            {
                Text = "Exclude Processes (comma-separated):",
                Location = new Point(12, 110),
                Size = new Size(250, 23)
            };

            excludedProcessesTextBox = new TextBox
            {
                Location = new Point(12, 136),
                Size = new Size(450, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            primaryMonitorOnlyCheckBox = new CheckBox
            {
                Text = "Primary monitor only",
                Location = new Point(12, 210),
                Size = new Size(200, 23)
            };

            filterTab.Controls.AddRange(new Control[]
            {
                processLabel, processNamesTextBox,
                excludeLabel, excludedProcessesTextBox,
                primaryMonitorOnlyCheckBox
            });

            tabControl.TabPages.Add(filterTab);
        }

        private void CreateButtonPanel()
        {
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            resetButton = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(12, 12),
                Size = new Size(120, 26)
            };

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(318, 12),
                Size = new Size(75, 26),
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(399, 12),
                Size = new Size(75, 26),
                DialogResult = DialogResult.Cancel
            };

            resetButton.Click += ResetButton_Click;
            okButton.Click += OkButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { resetButton, okButton, cancelButton });
            this.Controls.Add(buttonPanel);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadSettings()
        {
            leftMarginUpDown.Value = MarginSettings.LeftMargin;
            topMarginUpDown.Value = MarginSettings.TopMargin;
            rightMarginUpDown.Value = MarginSettings.RightMargin;
            bottomMarginUpDown.Value = MarginSettings.BottomMargin;
            usePercentageCheckBox.Checked = MarginSettings.UsePercentage;
            enableAnimationsCheckBox.Checked = MarginSettings.EnableAnimations;
            animationDurationUpDown.Value = MarginSettings.AnimationDuration;
            marginTypeComboBox.SelectedItem = MarginSettings.MarginType.ToString();
            easingComboBox.SelectedItem = MarginSettings.AnimationEasing.ToString();

            if (WindowFilter.ProcessNames != null)
                processNamesTextBox.Text = string.Join(", ", WindowFilter.ProcessNames);
            if (WindowFilter.ExcludedProcesses != null)
                excludedProcessesTextBox.Text = string.Join(", ", WindowFilter.ExcludedProcesses);
            primaryMonitorOnlyCheckBox.Checked = WindowFilter.PrimaryMonitorOnly;
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            MarginSettings = new MarginSettings();
            WindowFilter = new WindowFilterCriteria();
            LoadSettings();
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            MarginSettings.LeftMargin = (int)leftMarginUpDown.Value;
            MarginSettings.TopMargin = (int)topMarginUpDown.Value;
            MarginSettings.RightMargin = (int)rightMarginUpDown.Value;
            MarginSettings.BottomMargin = (int)bottomMarginUpDown.Value;
            MarginSettings.UsePercentage = usePercentageCheckBox.Checked;
            MarginSettings.EnableAnimations = enableAnimationsCheckBox.Checked;
            MarginSettings.AnimationDuration = (int)animationDurationUpDown.Value;
            
            if (Enum.TryParse<MarginType>(marginTypeComboBox.SelectedItem?.ToString(), out var marginType))
                MarginSettings.MarginType = marginType;
            if (Enum.TryParse<EasingType>(easingComboBox.SelectedItem?.ToString(), out var easingType))
                MarginSettings.AnimationEasing = easingType;

            WindowFilter.ProcessNames = processNamesTextBox.Text
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            WindowFilter.ExcludedProcesses = excludedProcessesTextBox.Text
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            WindowFilter.PrimaryMonitorOnly = primaryMonitorOnlyCheckBox.Checked;
        }
    }
}
