using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class SchedulingEngine : IDisposable
    {
        private readonly string schedulesPath;
        private readonly WindowManager windowManager;
        private readonly ProfileManager profileManager;
        private readonly MultiMonitorManager multiMonitorManager;
        private readonly Timer schedulerTimer;
        private List<ScheduledTask> scheduledTasks;
        private readonly SystemEventMonitor eventMonitor;
        private bool disposed = false;

        public event EventHandler<ScheduledTaskEventArgs>? TaskExecuted;
        public event EventHandler<ScheduledTaskEventArgs>? TaskFailed;

        public SchedulingEngine(WindowManager windowManager, ProfileManager profileManager, MultiMonitorManager multiMonitorManager)
        {
            this.windowManager = windowManager;
            this.profileManager = profileManager;
            this.multiMonitorManager = multiMonitorManager;
            
            schedulesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowsMarginManager",
                "schedules.json"
            );

            scheduledTasks = new List<ScheduledTask>();
            eventMonitor = new SystemEventMonitor();
            
            schedulerTimer = new Timer(60000); // Check every minute
            schedulerTimer.Elapsed += OnSchedulerTick;
            schedulerTimer.AutoReset = true;
            
            LoadScheduledTasks();
            SetupEventMonitoring();
            schedulerTimer.Start();
        }

        public List<ScheduledTask> GetScheduledTasks() => new List<ScheduledTask>(scheduledTasks);

        public void AddScheduledTask(ScheduledTask task)
        {
            task.Id = task.Id ?? Guid.NewGuid().ToString();
            task.Created = DateTime.UtcNow;
            task.LastModified = DateTime.UtcNow;
            
            var existing = scheduledTasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                scheduledTasks.Remove(existing);
            }
            
            scheduledTasks.Add(task);
            SaveScheduledTasks();
        }

        public void RemoveScheduledTask(string taskId)
        {
            var task = scheduledTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                scheduledTasks.Remove(task);
                SaveScheduledTasks();
            }
        }

        public void EnableTask(string taskId, bool enabled)
        {
            var task = scheduledTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Enabled = enabled;
                task.LastModified = DateTime.UtcNow;
                SaveScheduledTasks();
            }
        }

        private void OnSchedulerTick(object? sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var tasksToExecute = scheduledTasks.Where(task => 
                task.Enabled && ShouldExecuteTask(task, now)).ToList();

            foreach (var task in tasksToExecute)
            {
                ExecuteTask(task);
            }
        }

        private bool ShouldExecuteTask(ScheduledTask task, DateTime now)
        {
            if (!task.Enabled) return false;

            switch (task.TriggerType)
            {
                case TriggerType.Time:
                    return ShouldExecuteTimeBasedTask(task, now);
                case TriggerType.Interval:
                    return ShouldExecuteIntervalTask(task, now);
                case TriggerType.Daily:
                    return ShouldExecuteDailyTask(task, now);
                case TriggerType.Weekly:
                    return ShouldExecuteWeeklyTask(task, now);
                case TriggerType.Startup:
                    return task.LastExecuted == null;
                default:
                    return false;
            }
        }

        private bool ShouldExecuteTimeBasedTask(ScheduledTask task, DateTime now)
        {
            if (task.ScheduledTime == null) return false;
            
            var scheduledTime = task.ScheduledTime.Value;
            var timeDiff = Math.Abs((now - scheduledTime).TotalMinutes);
            
            return timeDiff < 1 && (task.LastExecuted == null || 
                (now - task.LastExecuted.Value).TotalHours > 23);
        }

        private bool ShouldExecuteIntervalTask(ScheduledTask task, DateTime now)
        {
            if (task.IntervalMinutes == null || task.IntervalMinutes <= 0) return false;
            
            if (task.LastExecuted == null) return true;
            
            var minutesSinceLastExecution = (now - task.LastExecuted.Value).TotalMinutes;
            return minutesSinceLastExecution >= task.IntervalMinutes.Value;
        }

        private bool ShouldExecuteDailyTask(ScheduledTask task, DateTime now)
        {
            if (task.DailyTime == null) return false;
            
            var todayScheduled = now.Date.Add(task.DailyTime.Value);
            var timeDiff = Math.Abs((now - todayScheduled).TotalMinutes);
            
            return timeDiff < 1 && (task.LastExecuted == null || 
                task.LastExecuted.Value.Date < now.Date);
        }

        private bool ShouldExecuteWeeklyTask(ScheduledTask task, DateTime now)
        {
            if (task.WeeklyDays == null || !task.WeeklyDays.Any() || task.WeeklyTime == null) 
                return false;
            
            if (!task.WeeklyDays.Contains(now.DayOfWeek)) return false;
            
            var todayScheduled = now.Date.Add(task.WeeklyTime.Value);
            var timeDiff = Math.Abs((now - todayScheduled).TotalMinutes);
            
            return timeDiff < 1 && (task.LastExecuted == null || 
                task.LastExecuted.Value.Date < now.Date);
        }

        private async void ExecuteTask(ScheduledTask task)
        {
            try
            {
                task.LastExecuted = DateTime.UtcNow;
                task.ExecutionCount++;

                switch (task.ActionType)
                {
                    case ScheduledActionType.ApplyProfile:
                        await ExecuteApplyProfileAction(task);
                        break;
                    case ScheduledActionType.ApplyMargins:
                        await ExecuteApplyMarginsAction(task);
                        break;
                    case ScheduledActionType.RestoreWindows:
                        await ExecuteRestoreWindowsAction(task);
                        break;
                    case ScheduledActionType.TileWindows:
                        await ExecuteTileWindowsAction(task);
                        break;
                    case ScheduledActionType.DistributeAcrossMonitors:
                        await ExecuteDistributeAction(task);
                        break;
                    case ScheduledActionType.CustomScript:
                        await ExecuteCustomScriptAction(task);
                        break;
                }

                SaveScheduledTasks();
                TaskExecuted?.Invoke(this, new ScheduledTaskEventArgs { Task = task, Success = true });
            }
            catch (Exception ex)
            {
                task.LastError = ex.Message;
                task.ErrorCount++;
                SaveScheduledTasks();
                TaskFailed?.Invoke(this, new ScheduledTaskEventArgs { Task = task, Success = false, Error = ex.Message });
            }
        }

        private async System.Threading.Tasks.Task ExecuteApplyProfileAction(ScheduledTask task)
        {
            if (string.IsNullOrEmpty(task.ProfileName)) return;
            
            var profile = profileManager.GetProfile(task.ProfileName);
            if (profile != null)
            {
                var windows = windowManager.GetTopLevelWindows(profile.WindowFilter ?? new WindowFilterCriteria());
                foreach (var window in windows)
                {
                    var targetBounds = multiMonitorManager.CalculateOptimalWindowBounds(window.Handle, profile.MarginSettings);
                    if (targetBounds != System.Drawing.Rectangle.Empty)
                    {
                        windowManager.SetWindowPosition(window.Handle, targetBounds);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task ExecuteApplyMarginsAction(ScheduledTask task)
        {
            if (task.MarginSettings == null) return;
            
            var windows = windowManager.GetTopLevelWindows(task.WindowFilter ?? new WindowFilterCriteria());
            foreach (var window in windows)
            {
                var targetBounds = multiMonitorManager.CalculateOptimalWindowBounds(window.Handle, task.MarginSettings);
                if (targetBounds != System.Drawing.Rectangle.Empty)
                {
                    windowManager.SetWindowPosition(window.Handle, targetBounds);
                }
            }
        }

        private async System.Threading.Tasks.Task ExecuteRestoreWindowsAction(ScheduledTask task)
        {
            var windows = windowManager.GetTopLevelWindows(task.WindowFilter ?? new WindowFilterCriteria());
            foreach (var window in windows)
            {
                if (window.IsMaximized)
                {
                    windowManager.RestoreWindow(window.Handle);
                }
            }
        }

        private async System.Threading.Tasks.Task ExecuteTileWindowsAction(ScheduledTask task)
        {
            var windows = windowManager.GetTopLevelWindows(task.WindowFilter ?? new WindowFilterCriteria());
            if (windows.Any())
            {
                var primaryMonitor = multiMonitorManager.GetPrimaryMonitor();
                if (primaryMonitor != null)
                {
                    var bounds = multiMonitorManager.CalculateOptimalWindowBounds(
                        windows.First().Handle, task.MarginSettings ?? new MarginSettings());
                    windowManager.TileWindows(windows, bounds, task.TileLayout ?? TileLayout.Grid);
                }
            }
        }

        private async System.Threading.Tasks.Task ExecuteDistributeAction(ScheduledTask task)
        {
            var windows = windowManager.GetTopLevelWindows(task.WindowFilter ?? new WindowFilterCriteria());
            var monitors = multiMonitorManager.GetMonitors();
            
            if (windows.Count > 0 && monitors.Count > 1)
            {
                multiMonitorManager.DistributeWindowsAcrossMonitors(windows);
                foreach (var window in windows)
                {
                    if (window.TargetBounds != System.Drawing.Rectangle.Empty)
                    {
                        windowManager.SetWindowPosition(window.Handle, window.TargetBounds);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task ExecuteCustomScriptAction(ScheduledTask task)
        {
            if (string.IsNullOrEmpty(task.CustomScript)) return;
            
            await System.Threading.Tasks.Task.Delay(100); // Placeholder
        }

        private void SetupEventMonitoring()
        {
            eventMonitor.ApplicationLaunched += OnApplicationLaunched;
            eventMonitor.MonitorConfigurationChanged += OnMonitorConfigurationChanged;
            eventMonitor.SystemResumed += OnSystemResumed;
            eventMonitor.UserLoggedIn += OnUserLoggedIn;
        }

        private void OnApplicationLaunched(object? sender, ApplicationEventArgs e)
        {
            var eventTasks = scheduledTasks.Where(t => 
                t.Enabled && 
                t.TriggerType == TriggerType.ApplicationLaunch &&
                (t.TriggerApplications?.Contains(e.ProcessName, StringComparer.OrdinalIgnoreCase) ?? false)
            ).ToList();

            foreach (var task in eventTasks)
            {
                ExecuteTask(task);
            }
        }

        private void OnMonitorConfigurationChanged(object? sender, EventArgs e)
        {
            var eventTasks = scheduledTasks.Where(t => 
                t.Enabled && t.TriggerType == TriggerType.MonitorChange).ToList();

            foreach (var task in eventTasks)
            {
                ExecuteTask(task);
            }
        }

        private void OnSystemResumed(object? sender, EventArgs e)
        {
            var eventTasks = scheduledTasks.Where(t => 
                t.Enabled && t.TriggerType == TriggerType.SystemResume).ToList();

            foreach (var task in eventTasks)
            {
                ExecuteTask(task);
            }
        }

        private void OnUserLoggedIn(object? sender, EventArgs e)
        {
            var eventTasks = scheduledTasks.Where(t => 
                t.Enabled && t.TriggerType == TriggerType.UserLogin).ToList();

            foreach (var task in eventTasks)
            {
                ExecuteTask(task);
            }
        }

        private void LoadScheduledTasks()
        {
            try
            {
                if (File.Exists(schedulesPath))
                {
                    var json = File.ReadAllText(schedulesPath);
                    var loadedTasks = JsonConvert.DeserializeObject<List<ScheduledTask>>(json);
                    if (loadedTasks != null)
                    {
                        scheduledTasks = loadedTasks;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading scheduled tasks: {ex.Message}");
            }
        }

        private void SaveScheduledTasks()
        {
            try
            {
                var directory = Path.GetDirectoryName(schedulesPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(scheduledTasks, Formatting.Indented);
                File.WriteAllText(schedulesPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving scheduled tasks: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                schedulerTimer?.Stop();
                schedulerTimer?.Dispose();
                eventMonitor?.Dispose();
                disposed = true;
            }
        }
    }

    public class ScheduledTask
    {
        public string? Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public TriggerType TriggerType { get; set; }
        public ScheduledActionType ActionType { get; set; }
        
        public DateTime? ScheduledTime { get; set; }
        public TimeSpan? DailyTime { get; set; }
        public TimeSpan? WeeklyTime { get; set; }
        public List<DayOfWeek>? WeeklyDays { get; set; }
        public int? IntervalMinutes { get; set; }
        
        public List<string>? TriggerApplications { get; set; }
        
        public string? ProfileName { get; set; }
        public MarginSettings? MarginSettings { get; set; }
        public WindowFilterCriteria? WindowFilter { get; set; }
        public TileLayout? TileLayout { get; set; }
        public string? CustomScript { get; set; }
        
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public DateTime? LastExecuted { get; set; }
        public int ExecutionCount { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;
        public string? LastError { get; set; }
        
        public List<string>? RequiredApplications { get; set; }
        public List<string>? ExcludedApplications { get; set; }
        public bool RequireUserPresent { get; set; } = false;
        public TimeSpan? MinTimeSinceLastExecution { get; set; }
        public int? MaxExecutionsPerDay { get; set; }
    }

    public enum TriggerType
    {
        Time,
        Daily,
        Weekly,
        Interval,
        Startup,
        ApplicationLaunch,
        MonitorChange,
        SystemResume,
        UserLogin
    }

    public enum ScheduledActionType
    {
        ApplyProfile,
        ApplyMargins,
        RestoreWindows,
        TileWindows,
        DistributeAcrossMonitors,
        CustomScript
    }

    public class ScheduledTaskEventArgs : EventArgs
    {
        public ScheduledTask Task { get; set; } = new ScheduledTask();
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
