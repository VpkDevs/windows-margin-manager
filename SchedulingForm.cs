using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class SchedulingForm : Form
    {
        private readonly SchedulingEngine schedulingEngine;
        private readonly ProfileManager profileManager;
        
        private ListView taskListView;
        private Button newTaskButton, editTaskButton, deleteTaskButton, enableDisableButton;
        private Button okButton, cancelButton;
        private GroupBox taskDetailsGroupBox;
        private Label nameLabel, triggerLabel, actionLabel, statusLabel, lastExecutedLabel;
        private TextBox logTextBox;

        public SchedulingForm(SchedulingEngine schedulingEngine, ProfileManager profileManager)
        {
            this.schedulingEngine = schedulingEngine;
            this.profileManager = profileManager;
            InitializeComponent();
            LoadScheduledTasks();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.Text = "Task Scheduler";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(700, 500);

            CreateTaskList();
            CreateDetailsPanel();
            CreateLogPanel();
            CreateButtonPanel();
        }

        private void CreateTaskList()
        {
            var listLabel = new Label
            {
                Text = "Scheduled Tasks:",
                Location = new Point(12, 12),
                Size = new Size(150, 23)
            };

            taskListView = new ListView
            {
                Location = new Point(12, 38),
                Size = new Size(400, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            taskListView.Columns.Add("Name", 150);
            taskListView.Columns.Add("Trigger", 100);
            taskListView.Columns.Add("Action", 100);
            taskListView.Columns.Add("Status", 50);
            taskListView.SelectedIndexChanged += TaskListView_SelectedIndexChanged;

            var listButtonPanel = new Panel
            {
                Location = new Point(12, 344),
                Size = new Size(400, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            newTaskButton = new Button
            {
                Text = "New Task",
                Location = new Point(0, 0),
                Size = new Size(80, 26)
            };

            editTaskButton = new Button
            {
                Text = "Edit",
                Location = new Point(85, 0),
                Size = new Size(60, 26),
                Enabled = false
            };

            deleteTaskButton = new Button
            {
                Text = "Delete",
                Location = new Point(150, 0),
                Size = new Size(60, 26),
                Enabled = false
            };

            enableDisableButton = new Button
            {
                Text = "Disable",
                Location = new Point(215, 0),
                Size = new Size(70, 26),
                Enabled = false
            };

            newTaskButton.Click += NewTaskButton_Click;
            editTaskButton.Click += EditTaskButton_Click;
            deleteTaskButton.Click += DeleteTaskButton_Click;
            enableDisableButton.Click += EnableDisableButton_Click;

            listButtonPanel.Controls.AddRange(new Control[] 
            { 
                newTaskButton, editTaskButton, deleteTaskButton, enableDisableButton 
            });

            this.Controls.AddRange(new Control[] { listLabel, taskListView, listButtonPanel });
        }

        private void CreateDetailsPanel()
        {
            taskDetailsGroupBox = new GroupBox
            {
                Text = "Task Details",
                Location = new Point(425, 12),
                Size = new Size(350, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            nameLabel = new Label
            {
                Text = "Name: Not selected",
                Location = new Point(12, 25),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            triggerLabel = new Label
            {
                Text = "Trigger: Not selected",
                Location = new Point(12, 50),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            actionLabel = new Label
            {
                Text = "Action: Not selected",
                Location = new Point(12, 75),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            statusLabel = new Label
            {
                Text = "Status: Not selected",
                Location = new Point(12, 100),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lastExecutedLabel = new Label
            {
                Text = "Last Executed: Not selected",
                Location = new Point(12, 125),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var executionLabel = new Label
            {
                Text = "Execution Count: Not selected",
                Location = new Point(12, 150),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            taskDetailsGroupBox.Controls.AddRange(new Control[]
            {
                nameLabel, triggerLabel, actionLabel, statusLabel, lastExecutedLabel, executionLabel
            });

            this.Controls.Add(taskDetailsGroupBox);
        }

        private void CreateLogPanel()
        {
            var logGroupBox = new GroupBox
            {
                Text = "Execution Log",
                Location = new Point(425, 225),
                Size = new Size(350, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            logTextBox = new TextBox
            {
                Location = new Point(12, 25),
                Size = new Size(326, 115),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8)
            };

            logGroupBox.Controls.Add(logTextBox);
            this.Controls.Add(logGroupBox);
        }

        private void CreateButtonPanel()
        {
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            okButton = new Button
            {
                Text = "Close",
                Location = new Point(700, 12),
                Size = new Size(75, 26),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            buttonPanel.Controls.Add(okButton);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = okButton;
        }

        private void LoadScheduledTasks()
        {
            taskListView.Items.Clear();
            var tasks = schedulingEngine.GetScheduledTasks();
            
            foreach (var task in tasks)
            {
                var item = new ListViewItem(task.Name);
                item.SubItems.Add(GetTriggerDescription(task));
                item.SubItems.Add(GetActionDescription(task));
                item.SubItems.Add(task.Enabled ? "Enabled" : "Disabled");
                item.Tag = task;
                
                if (!task.Enabled)
                {
                    item.ForeColor = Color.Gray;
                }
                
                taskListView.Items.Add(item);
            }
        }

        private string GetTriggerDescription(ScheduledTask task)
        {
            return task.TriggerType switch
            {
                TriggerType.Time => $"At {task.ScheduledTime:HH:mm}",
                TriggerType.Daily => $"Daily at {task.DailyTime:hh\\:mm}",
                TriggerType.Weekly => $"Weekly {string.Join(",", task.WeeklyDays ?? new System.Collections.Generic.List<DayOfWeek>())} at {task.WeeklyTime:hh\\:mm}",
                TriggerType.Interval => $"Every {task.IntervalMinutes} min",
                TriggerType.Startup => "At startup",
                TriggerType.ApplicationLaunch => "App launch",
                TriggerType.MonitorChange => "Monitor change",
                TriggerType.SystemResume => "System resume",
                TriggerType.UserLogin => "User login",
                _ => "Unknown"
            };
        }

        private string GetActionDescription(ScheduledTask task)
        {
            return task.ActionType switch
            {
                ScheduledActionType.ApplyProfile => $"Apply '{task.ProfileName}'",
                ScheduledActionType.ApplyMargins => "Apply margins",
                ScheduledActionType.RestoreWindows => "Restore windows",
                ScheduledActionType.TileWindows => "Tile windows",
                ScheduledActionType.DistributeAcrossMonitors => "Distribute windows",
                ScheduledActionType.CustomScript => "Run script",
                _ => "Unknown"
            };
        }

        private void TaskListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = taskListView.SelectedItems.Count > 0;
            editTaskButton.Enabled = hasSelection;
            deleteTaskButton.Enabled = hasSelection;
            enableDisableButton.Enabled = hasSelection;

            if (hasSelection && taskListView.SelectedItems[0].Tag is ScheduledTask task)
            {
                ShowTaskDetails(task);
                enableDisableButton.Text = task.Enabled ? "Disable" : "Enable";
            }
            else
            {
                ClearTaskDetails();
            }
        }

        private void ShowTaskDetails(ScheduledTask task)
        {
            nameLabel.Text = $"Name: {task.Name}";
            triggerLabel.Text = $"Trigger: {GetTriggerDescription(task)}";
            actionLabel.Text = $"Action: {GetActionDescription(task)}";
            statusLabel.Text = $"Status: {(task.Enabled ? "Enabled" : "Disabled")}";
            lastExecutedLabel.Text = $"Last Executed: {(task.LastExecuted?.ToString("yyyy-MM-dd HH:mm") ?? "Never")}";
            
            var logText = $"Execution Count: {task.ExecutionCount}\n";
            logText += $"Error Count: {task.ErrorCount}\n";
            if (!string.IsNullOrEmpty(task.LastError))
            {
                logText += $"Last Error: {task.LastError}\n";
            }
            logText += $"Created: {task.Created:yyyy-MM-dd HH:mm}\n";
            logText += $"Modified: {task.LastModified:yyyy-MM-dd HH:mm}";
            
            logTextBox.Text = logText;
        }

        private void ClearTaskDetails()
        {
            nameLabel.Text = "Name: Not selected";
            triggerLabel.Text = "Trigger: Not selected";
            actionLabel.Text = "Action: Not selected";
            statusLabel.Text = "Status: Not selected";
            lastExecutedLabel.Text = "Last Executed: Not selected";
            logTextBox.Text = "";
        }

        private void NewTaskButton_Click(object? sender, EventArgs e)
        {
            var taskForm = new TaskEditorForm(null, profileManager);
            if (taskForm.ShowDialog() == DialogResult.OK && taskForm.Task != null)
            {
                schedulingEngine.AddScheduledTask(taskForm.Task);
                LoadScheduledTasks();
            }
        }

        private void EditTaskButton_Click(object? sender, EventArgs e)
        {
            if (taskListView.SelectedItems.Count > 0 && 
                taskListView.SelectedItems[0].Tag is ScheduledTask task)
            {
                var taskForm = new TaskEditorForm(task, profileManager);
                if (taskForm.ShowDialog() == DialogResult.OK && taskForm.Task != null)
                {
                    schedulingEngine.AddScheduledTask(taskForm.Task);
                    LoadScheduledTasks();
                }
            }
        }

        private void DeleteTaskButton_Click(object? sender, EventArgs e)
        {
            if (taskListView.SelectedItems.Count > 0 && 
                taskListView.SelectedItems[0].Tag is ScheduledTask task)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the task '{task.Name}'?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    schedulingEngine.RemoveScheduledTask(task.Id!);
                    LoadScheduledTasks();
                    ClearTaskDetails();
                }
            }
        }

        private void EnableDisableButton_Click(object? sender, EventArgs e)
        {
            if (taskListView.SelectedItems.Count > 0 && 
                taskListView.SelectedItems[0].Tag is ScheduledTask task)
            {
                schedulingEngine.EnableTask(task.Id!, !task.Enabled);
                LoadScheduledTasks();
                
                var index = taskListView.SelectedIndices[0];
                taskListView.Items[index].Selected = true;
            }
        }

        private void SetupEventHandlers()
        {
            schedulingEngine.TaskExecuted += OnTaskExecuted;
            schedulingEngine.TaskFailed += OnTaskFailed;
        }

        private void OnTaskExecuted(object? sender, ScheduledTaskEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTaskExecuted(sender, e)));
                return;
            }

            LoadScheduledTasks();
            if (taskListView.SelectedItems.Count > 0 && 
                taskListView.SelectedItems[0].Tag is ScheduledTask selectedTask &&
                selectedTask.Id == e.Task.Id)
            {
                ShowTaskDetails(e.Task);
            }
        }

        private void OnTaskFailed(object? sender, ScheduledTaskEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTaskFailed(sender, e)));
                return;
            }

            LoadScheduledTasks();
            if (taskListView.SelectedItems.Count > 0 && 
                taskListView.SelectedItems[0].Tag is ScheduledTask selectedTask &&
                selectedTask.Id == e.Task.Id)
            {
                ShowTaskDetails(e.Task);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                schedulingEngine.TaskExecuted -= OnTaskExecuted;
                schedulingEngine.TaskFailed -= OnTaskFailed;
            }
            base.Dispose(disposing);
        }
    }
}
