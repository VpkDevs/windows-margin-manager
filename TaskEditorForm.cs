using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class TaskEditorForm : Form
    {
        public ScheduledTask? Task { get; private set; }
        
        private readonly ProfileManager profileManager;
        private readonly ScheduledTask? originalTask;
        
        private TextBox nameTextBox, descriptionTextBox;
        private ComboBox triggerTypeComboBox, actionTypeComboBox, profileComboBox;
        private DateTimePicker scheduledTimePicker, dailyTimePicker, weeklyTimePicker;
        private NumericUpDown intervalUpDown;
        private CheckedListBox weeklyDaysCheckedListBox;
        private TextBox triggerApplicationsTextBox;
        private Panel triggerPanel, actionPanel;
        private CheckBox enabledCheckBox;
        private Button okButton, cancelButton;

        public TaskEditorForm(ScheduledTask? task, ProfileManager profileManager)
        {
            this.originalTask = task;
            this.profileManager = profileManager;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = originalTask == null ? "New Scheduled Task" : "Edit Scheduled Task";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            CreateBasicInfoPanel();
            CreateTriggerPanel();
            CreateActionPanel();
            CreateButtonPanel();
        }

        private void CreateBasicInfoPanel()
        {
            var basicGroupBox = new GroupBox
            {
                Text = "Basic Information",
                Location = new Point(12, 12),
                Size = new Size(460, 120)
            };

            var nameLabel = new Label
            {
                Text = "Task Name:",
                Location = new Point(12, 25),
                Size = new Size(80, 23)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(98, 22),
                Size = new Size(350, 23)
            };

            var descLabel = new Label
            {
                Text = "Description:",
                Location = new Point(12, 55),
                Size = new Size(80, 23)
            };

            descriptionTextBox = new TextBox
            {
                Location = new Point(98, 52),
                Size = new Size(350, 40),
                Multiline = true
            };

            enabledCheckBox = new CheckBox
            {
                Text = "Enabled",
                Location = new Point(98, 98),
                Size = new Size(100, 23),
                Checked = true
            };

            basicGroupBox.Controls.AddRange(new Control[]
            {
                nameLabel, nameTextBox, descLabel, descriptionTextBox, enabledCheckBox
            });

            this.Controls.Add(basicGroupBox);
        }

        private void CreateTriggerPanel()
        {
            var triggerGroupBox = new GroupBox
            {
                Text = "Trigger",
                Location = new Point(12, 145),
                Size = new Size(460, 200)
            };

            var triggerTypeLabel = new Label
            {
                Text = "Trigger Type:",
                Location = new Point(12, 25),
                Size = new Size(80, 23)
            };

            triggerTypeComboBox = new ComboBox
            {
                Location = new Point(98, 22),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            triggerTypeComboBox.Items.AddRange(Enum.GetNames(typeof(TriggerType)));
            triggerTypeComboBox.SelectedIndexChanged += TriggerTypeComboBox_SelectedIndexChanged;

            triggerPanel = new Panel
            {
                Location = new Point(12, 55),
                Size = new Size(436, 135),
                BorderStyle = BorderStyle.FixedSingle
            };

            CreateTriggerControls();

            triggerGroupBox.Controls.AddRange(new Control[]
            {
                triggerTypeLabel, triggerTypeComboBox, triggerPanel
            });

            this.Controls.Add(triggerGroupBox);
        }

        private void CreateTriggerControls()
        {
            var timeLabel = new Label
            {
                Text = "Time:",
                Location = new Point(12, 15),
                Size = new Size(50, 23),
                Visible = false
            };

            scheduledTimePicker = new DateTimePicker
            {
                Location = new Point(68, 12),
                Size = new Size(200, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Visible = false
            };

            var dailyLabel = new Label
            {
                Text = "Daily at:",
                Location = new Point(12, 15),
                Size = new Size(60, 23),
                Visible = false
            };

            dailyTimePicker = new DateTimePicker
            {
                Location = new Point(78, 12),
                Size = new Size(100, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Visible = false
            };

            var weeklyLabel = new Label
            {
                Text = "Weekly:",
                Location = new Point(12, 15),
                Size = new Size(60, 23),
                Visible = false
            };

            weeklyDaysCheckedListBox = new CheckedListBox
            {
                Location = new Point(12, 45),
                Size = new Size(200, 80),
                CheckOnClick = true,
                Visible = false
            };
            weeklyDaysCheckedListBox.Items.AddRange(Enum.GetNames(typeof(DayOfWeek)));

            weeklyTimePicker = new DateTimePicker
            {
                Location = new Point(220, 45),
                Size = new Size(100, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Visible = false
            };

            var intervalLabel = new Label
            {
                Text = "Every:",
                Location = new Point(12, 15),
                Size = new Size(50, 23),
                Visible = false
            };

            intervalUpDown = new NumericUpDown
            {
                Location = new Point(68, 12),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 1440,
                Value = 60,
                Visible = false
            };

            var minutesLabel = new Label
            {
                Text = "minutes",
                Location = new Point(154, 15),
                Size = new Size(60, 23),
                Visible = false
            };

            var appLabel = new Label
            {
                Text = "Applications (comma-separated):",
                Location = new Point(12, 15),
                Size = new Size(200, 23),
                Visible = false
            };

            triggerApplicationsTextBox = new TextBox
            {
                Location = new Point(12, 45),
                Size = new Size(400, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Visible = false
            };

            triggerPanel.Controls.AddRange(new Control[]
            {
                timeLabel, scheduledTimePicker,
                dailyLabel, dailyTimePicker,
                weeklyLabel, weeklyDaysCheckedListBox, weeklyTimePicker,
                intervalLabel, intervalUpDown, minutesLabel,
                appLabel, triggerApplicationsTextBox
            });
        }

        private void CreateActionPanel()
        {
            var actionGroupBox = new GroupBox
            {
                Text = "Action",
                Location = new Point(12, 360),
                Size = new Size(460, 120)
            };

            var actionTypeLabel = new Label
            {
                Text = "Action Type:",
                Location = new Point(12, 25),
                Size = new Size(80, 23)
            };

            actionTypeComboBox = new ComboBox
            {
                Location = new Point(98, 22),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            actionTypeComboBox.Items.AddRange(Enum.GetNames(typeof(ScheduledActionType)));
            actionTypeComboBox.SelectedIndexChanged += ActionTypeComboBox_SelectedIndexChanged;

            var profileLabel = new Label
            {
                Text = "Profile:",
                Location = new Point(12, 60),
                Size = new Size(80, 23),
                Visible = false
            };

            profileComboBox = new ComboBox
            {
                Location = new Point(98, 57),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };

            actionPanel = new Panel
            {
                Location = new Point(12, 55),
                Size = new Size(436, 55)
            };

            actionPanel.Controls.AddRange(new Control[] { profileLabel, profileComboBox });

            actionGroupBox.Controls.AddRange(new Control[]
            {
                actionTypeLabel, actionTypeComboBox, actionPanel
            });

            this.Controls.Add(actionGroupBox);
        }

        private void CreateButtonPanel()
        {
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(316, 500),
                Size = new Size(75, 26),
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(397, 500),
                Size = new Size(75, 26),
                DialogResult = DialogResult.Cancel
            };

            okButton.Click += OkButton_Click;

            this.Controls.AddRange(new Control[] { okButton, cancelButton });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadData()
        {
            var profiles = profileManager.GetProfiles();
            profileComboBox.Items.Clear();
            foreach (var profile in profiles)
            {
                profileComboBox.Items.Add(profile.Name);
            }

            if (originalTask != null)
            {
                nameTextBox.Text = originalTask.Name;
                descriptionTextBox.Text = originalTask.Description;
                enabledCheckBox.Checked = originalTask.Enabled;
                triggerTypeComboBox.SelectedItem = originalTask.TriggerType.ToString();
                actionTypeComboBox.SelectedItem = originalTask.ActionType.ToString();

                if (!string.IsNullOrEmpty(originalTask.ProfileName))
                {
                    profileComboBox.SelectedItem = originalTask.ProfileName;
                }

                LoadTriggerData();
            }
            else
            {
                triggerTypeComboBox.SelectedIndex = 0;
                actionTypeComboBox.SelectedIndex = 0;
            }
        }

        private void LoadTriggerData()
        {
            if (originalTask == null) return;

            switch (originalTask.TriggerType)
            {
                case TriggerType.Time:
                    if (originalTask.ScheduledTime.HasValue)
                        scheduledTimePicker.Value = originalTask.ScheduledTime.Value;
                    break;
                case TriggerType.Daily:
                    if (originalTask.DailyTime.HasValue)
                        dailyTimePicker.Value = DateTime.Today.Add(originalTask.DailyTime.Value);
                    break;
                case TriggerType.Weekly:
                    if (originalTask.WeeklyDays != null)
                    {
                        for (int i = 0; i < weeklyDaysCheckedListBox.Items.Count; i++)
                        {
                            var dayName = weeklyDaysCheckedListBox.Items[i].ToString();
                            if (Enum.TryParse<DayOfWeek>(dayName, out var day) && originalTask.WeeklyDays.Contains(day))
                            {
                                weeklyDaysCheckedListBox.SetItemChecked(i, true);
                            }
                        }
                    }
                    if (originalTask.WeeklyTime.HasValue)
                        weeklyTimePicker.Value = DateTime.Today.Add(originalTask.WeeklyTime.Value);
                    break;
                case TriggerType.Interval:
                    if (originalTask.IntervalMinutes.HasValue)
                        intervalUpDown.Value = originalTask.IntervalMinutes.Value;
                    break;
                case TriggerType.ApplicationLaunch:
                    if (originalTask.TriggerApplications != null)
                        triggerApplicationsTextBox.Text = string.Join(", ", originalTask.TriggerApplications);
                    break;
            }
        }

        private void TriggerTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            HideAllTriggerControls();

            if (Enum.TryParse<TriggerType>(triggerTypeComboBox.SelectedItem?.ToString(), out var triggerType))
            {
                ShowTriggerControls(triggerType);
            }
        }

        private void ActionTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            HideAllActionControls();

            if (Enum.TryParse<ScheduledActionType>(actionTypeComboBox.SelectedItem?.ToString(), out var actionType))
            {
                ShowActionControls(actionType);
            }
        }

        private void HideAllTriggerControls()
        {
            foreach (Control control in triggerPanel.Controls)
            {
                control.Visible = false;
            }
        }

        private void ShowTriggerControls(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.Time:
                    triggerPanel.Controls[0].Visible = true; // timeLabel
                    triggerPanel.Controls[1].Visible = true; // scheduledTimePicker
                    break;
                case TriggerType.Daily:
                    triggerPanel.Controls[2].Visible = true; // dailyLabel
                    triggerPanel.Controls[3].Visible = true; // dailyTimePicker
                    break;
                case TriggerType.Weekly:
                    triggerPanel.Controls[4].Visible = true; // weeklyLabel
                    triggerPanel.Controls[5].Visible = true; // weeklyDaysCheckedListBox
                    triggerPanel.Controls[6].Visible = true; // weeklyTimePicker
                    break;
                case TriggerType.Interval:
                    triggerPanel.Controls[7].Visible = true; // intervalLabel
                    triggerPanel.Controls[8].Visible = true; // intervalUpDown
                    triggerPanel.Controls[9].Visible = true; // minutesLabel
                    break;
                case TriggerType.ApplicationLaunch:
                    triggerPanel.Controls[10].Visible = true; // appLabel
                    triggerPanel.Controls[11].Visible = true; // triggerApplicationsTextBox
                    break;
            }
        }

        private void HideAllActionControls()
        {
            foreach (Control control in actionPanel.Controls)
            {
                control.Visible = false;
            }
        }

        private void ShowActionControls(ScheduledActionType actionType)
        {
            if (actionType == ScheduledActionType.ApplyProfile)
            {
                actionPanel.Controls[0].Visible = true; // profileLabel
                actionPanel.Controls[1].Visible = true; // profileComboBox
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Please enter a task name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Enum.TryParse<TriggerType>(triggerTypeComboBox.SelectedItem?.ToString(), out var triggerType))
            {
                MessageBox.Show("Please select a trigger type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Enum.TryParse<ScheduledActionType>(actionTypeComboBox.SelectedItem?.ToString(), out var actionType))
            {
                MessageBox.Show("Please select an action type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Task = new ScheduledTask
            {
                Id = originalTask?.Id,
                Name = nameTextBox.Text.Trim(),
                Description = descriptionTextBox.Text.Trim(),
                Enabled = enabledCheckBox.Checked,
                TriggerType = triggerType,
                ActionType = actionType
            };

            switch (triggerType)
            {
                case TriggerType.Time:
                    Task.ScheduledTime = scheduledTimePicker.Value;
                    break;
                case TriggerType.Daily:
                    Task.DailyTime = dailyTimePicker.Value.TimeOfDay;
                    break;
                case TriggerType.Weekly:
                    Task.WeeklyDays = weeklyDaysCheckedListBox.CheckedItems.Cast<string>()
                        .Select(s => Enum.Parse<DayOfWeek>(s)).ToList();
                    Task.WeeklyTime = weeklyTimePicker.Value.TimeOfDay;
                    break;
                case TriggerType.Interval:
                    Task.IntervalMinutes = (int)intervalUpDown.Value;
                    break;
                case TriggerType.ApplicationLaunch:
                    Task.TriggerApplications = triggerApplicationsTextBox.Text
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    break;
            }

            if (actionType == ScheduledActionType.ApplyProfile)
            {
                Task.ProfileName = profileComboBox.SelectedItem?.ToString();
            }
        }
    }
}
