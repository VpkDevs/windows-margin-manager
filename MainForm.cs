using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private HotkeyManager hotkeyManager;
        private WindowManager windowManager;
        private MultiMonitorManager multiMonitorManager;
        private AnimationEngine animationEngine;
        private ProfileManager profileManager;
        private SchedulingEngine schedulingEngine;
        private MarginSettings marginSettings;
        private List<WindowInfo> originalWindowStates;
        private bool marginsApplied = false;
        private Timer statusUpdateTimer;
        private WindowFilterCriteria currentFilter;

        public MainForm()
        {
            InitializeComponent();
            InitializeManagers();
            InitializeTrayIcon();
            RegisterHotkeys();
            
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        private void InitializeComponent()
        {
            this.Text = "Windows Margin Manager Pro";
            this.Size = new Size(1, 1);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
        }

        private void InitializeManagers()
        {
            marginSettings = new MarginSettings();
            windowManager = new WindowManager();
            multiMonitorManager = new MultiMonitorManager();
            animationEngine = new AnimationEngine();
            profileManager = new ProfileManager();
            schedulingEngine = new SchedulingEngine(windowManager, profileManager, multiMonitorManager);
            hotkeyManager = new HotkeyManager(this);
            originalWindowStates = new List<WindowInfo>();
            currentFilter = new WindowFilterCriteria();
            
            multiMonitorManager.MonitorConfigurationChanged += OnMonitorConfigurationChanged;
            
            statusUpdateTimer = new Timer();
            statusUpdateTimer.Interval = 5000;
            statusUpdateTimer.Tick += UpdateTrayIconStatus;
            statusUpdateTimer.Start();
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            
            var toggleItem = new ToolStripMenuItem("Apply Margins (Ctrl+Alt+M)", null, OnToggleMargins);
            var restoreItem = new ToolStripMenuItem("Restore Windows (Ctrl+Alt+R)", null, OnRestoreWindows);
            var autoArrangeItem = new ToolStripMenuItem("Auto-Arrange (Ctrl+Alt+A)", null, OnAutoArrange);
            var tileItem = new ToolStripMenuItem("Tile Windows (Ctrl+Alt+T)", null, OnTileWindows);
            
            var profilesMenu = new ToolStripMenuItem("Profiles");
            BuildProfilesMenu(profilesMenu);
            
            var monitorsMenu = new ToolStripMenuItem("Monitors");
            BuildMonitorsMenu(monitorsMenu);
            
            var schedulingItem = new ToolStripMenuItem("Task Scheduler...", null, OnScheduling);
            var settingsItem = new ToolStripMenuItem("Settings", null, OnSettings);
            var aboutItem = new ToolStripMenuItem("About", null, OnAbout);
            var exitItem = new ToolStripMenuItem("Exit", null, OnExit);
            
            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                toggleItem,
                restoreItem,
                new ToolStripSeparator(),
                autoArrangeItem,
                tileItem,
                new ToolStripSeparator(),
                profilesMenu,
                monitorsMenu,
                new ToolStripSeparator(),
                schedulingItem,
                settingsItem,
                aboutItem,
                new ToolStripSeparator(),
                exitItem
            });

            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "Windows Margin Manager Pro"
            };
            
            trayIcon.DoubleClick += OnToggleMargins;
        }

        private void BuildProfilesMenu(ToolStripMenuItem profilesMenu)
        {
            profilesMenu.DropDownItems.Clear();
            
            var profiles = profileManager.GetProfiles();
            for (int i = 0; i < Math.Min(profiles.Count, 9); i++)
            {
                var profile = profiles[i];
                var item = new ToolStripMenuItem($"{i + 1}. {profile.Name} (Ctrl+Alt+{i + 1})", 
                    null, (s, e) => ApplyProfile(profile));
                profilesMenu.DropDownItems.Add(item);
            }
            
            if (profiles.Count > 0)
            {
                profilesMenu.DropDownItems.Add(new ToolStripSeparator());
            }
            
            profilesMenu.DropDownItems.Add(new ToolStripMenuItem("Manage Profiles...", null, OnManageProfiles));
        }

        private void BuildMonitorsMenu(ToolStripMenuItem monitorsMenu)
        {
            monitorsMenu.DropDownItems.Clear();
            
            var monitors = multiMonitorManager.GetMonitors();
            foreach (var monitor in monitors)
            {
                var monitorName = monitor.IsPrimary ? $"Primary - {monitor.DeviceName}" : monitor.DeviceName;
                var item = new ToolStripMenuItem(monitorName);
                
                item.DropDownItems.Add(new ToolStripMenuItem("Configure...", 
                    null, (s, e) => ConfigureMonitor(monitor)));
                item.DropDownItems.Add(new ToolStripMenuItem("Move Windows Here", 
                    null, (s, e) => MoveWindowsToMonitor(monitor)));
                
                monitorsMenu.DropDownItems.Add(item);
            }
            
            if (monitors.Count > 1)
            {
                monitorsMenu.DropDownItems.Add(new ToolStripSeparator());
                monitorsMenu.DropDownItems.Add(new ToolStripMenuItem("Distribute Across All", 
                    null, OnDistributeAcrossMonitors));
            }
        }

        private void RegisterHotkeys()
        {
            hotkeyManager.RegisterHotkey(Keys.M, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnToggleMargins);
            hotkeyManager.RegisterHotkey(Keys.R, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnRestoreWindows);
            hotkeyManager.RegisterHotkey(Keys.A, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnAutoArrange);
            hotkeyManager.RegisterHotkey(Keys.T, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnTileWindows);
            hotkeyManager.RegisterHotkey(Keys.S, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnSaveCurrentLayout);
            hotkeyManager.RegisterHotkey(Keys.F, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnToggleFullscreen);
            
            for (int i = 1; i <= 9; i++)
            {
                var keyCode = (Keys)(Keys.D0 + i);
                var profileIndex = i - 1;
                hotkeyManager.RegisterHotkey(keyCode, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, 
                    () => ApplyProfileByIndex(profileIndex));
            }
            
            hotkeyManager.RegisterHotkey(Keys.M, HotkeyManager.MOD_WIN | HotkeyManager.MOD_SHIFT, OnDistributeAcrossMonitors);
        }

        private void OnToggleMargins(object? sender, EventArgs e)
        {
            if (marginsApplied)
            {
                RestoreWindows();
            }
            else
            {
                ApplyMargins();
            }
        }

        private void OnRestoreWindows(object? sender, EventArgs e)
        {
            RestoreWindows();
        }

        private void OnAutoArrange(object? sender, EventArgs e)
        {
            AutoArrangeWindows();
        }

        private void OnTileWindows(object? sender, EventArgs e)
        {
            TileWindows();
        }

        private void OnSaveCurrentLayout(object? sender, EventArgs e)
        {
            SaveCurrentLayoutAsProfile();
        }

        private void OnToggleFullscreen(object? sender, EventArgs e)
        {
            ToggleFullscreenMode();
        }

        private void OnDistributeAcrossMonitors(object? sender, EventArgs e)
        {
            DistributeWindowsAcrossMonitors();
        }

        private async void ApplyMargins()
        {
            try
            {
                originalWindowStates.Clear();
                var windows = windowManager.GetTopLevelWindows(currentFilter);
                
                if (!windows.Any())
                {
                    ShowNotification("No windows found to apply margins to.", ToolTipIcon.Info);
                    return;
                }

                var animationTasks = new List<Task>();
                
                foreach (var window in windows)
                {
                    originalWindowStates.Add(new WindowInfo
                    {
                        Handle = window.Handle,
                        Rectangle = window.Rectangle,
                        IsMaximized = window.IsMaximized,
                        Placement = window.Placement,
                        WindowState = window.WindowState
                    });
                    
                    var targetBounds = multiMonitorManager.CalculateOptimalWindowBounds(window.Handle, marginSettings);
                    if (targetBounds != Rectangle.Empty)
                    {
                        if (marginSettings.EnableAnimations)
                        {
                            animationTasks.Add(animationEngine.AnimateWindowAsync(
                                window.Handle, window.Rectangle, targetBounds, marginSettings.AnimationDuration));
                        }
                        else
                        {
                            windowManager.SetWindowPosition(window.Handle, targetBounds);
                        }
                    }
                }
                
                if (animationTasks.Any())
                {
                    await Task.WhenAll(animationTasks);
                }
                
                marginsApplied = true;
                UpdateTrayIconStatus(null, EventArgs.Empty);
                ShowNotification($"Applied margins to {windows.Count} windows", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error applying margins: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private async void RestoreWindows()
        {
            try
            {
                if (!originalWindowStates.Any())
                {
                    ShowNotification("No windows to restore", ToolTipIcon.Warning);
                    return;
                }

                var animationTasks = new List<Task>();
                
                foreach (var windowInfo in originalWindowStates)
                {
                    if (windowInfo.IsMaximized)
                    {
                        windowManager.MaximizeWindow(windowInfo.Handle);
                    }
                    else
                    {
                        if (marginSettings.EnableAnimations)
                        {
                            var currentBounds = windowManager.GetWindowBounds(windowInfo.Handle);
                            animationTasks.Add(animationEngine.AnimateWindowAsync(
                                windowInfo.Handle, currentBounds, windowInfo.Rectangle, marginSettings.AnimationDuration));
                        }
                        else
                        {
                            windowManager.RestoreWindowPlacement(windowInfo.Handle, windowInfo.Placement);
                        }
                    }
                }
                
                if (animationTasks.Any())
                {
                    await Task.WhenAll(animationTasks);
                }
                
                originalWindowStates.Clear();
                marginsApplied = false;
                UpdateTrayIconStatus(null, EventArgs.Empty);
                ShowNotification("Windows restored to original positions", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error restoring windows: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void AutoArrangeWindows()
        {
            try
            {
                var windows = windowManager.GetTopLevelWindows(currentFilter);
                if (!windows.Any()) return;

                var monitors = multiMonitorManager.GetMonitors();
                if (monitors.Count > 1)
                {
                    multiMonitorManager.DistributeWindowsAcrossMonitors(windows);
                }
                
                foreach (var monitor in monitors)
                {
                    var monitorWindows = windows.Where(w => 
                        multiMonitorManager.GetMonitorFromWindow(w.Handle)?.Handle == monitor.Handle).ToList();
                    
                    if (monitorWindows.Any())
                    {
                        var optimalBounds = multiMonitorManager.CalculateOptimalWindowBounds(
                            monitorWindows.First().Handle, marginSettings);
                        windowManager.TileWindows(monitorWindows, optimalBounds, TileLayout.Grid);
                    }
                }
                
                ShowNotification($"Auto-arranged {windows.Count} windows", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error auto-arranging windows: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void TileWindows()
        {
            try
            {
                var windows = windowManager.GetTopLevelWindows(currentFilter);
                if (!windows.Any()) return;

                var primaryMonitor = multiMonitorManager.GetPrimaryMonitor();
                if (primaryMonitor != null)
                {
                    var bounds = multiMonitorManager.CalculateOptimalWindowBounds(
                        windows.First().Handle, marginSettings);
                    windowManager.TileWindows(windows, bounds, TileLayout.Grid);
                }
                
                ShowNotification($"Tiled {windows.Count} windows", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error tiling windows: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void DistributeWindowsAcrossMonitors()
        {
            try
            {
                var windows = windowManager.GetTopLevelWindows(currentFilter);
                var monitors = multiMonitorManager.GetMonitors();
                
                if (windows.Count == 0 || monitors.Count <= 1) return;

                multiMonitorManager.DistributeWindowsAcrossMonitors(windows);
                
                foreach (var window in windows)
                {
                    if (window.TargetBounds != Rectangle.Empty)
                    {
                        windowManager.SetWindowPosition(window.Handle, window.TargetBounds);
                    }
                }
                
                ShowNotification($"Distributed {windows.Count} windows across {monitors.Count} monitors", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error distributing windows: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void ApplyProfile(Profile profile)
        {
            try
            {
                marginSettings = profile.MarginSettings;
                currentFilter = profile.WindowFilter ?? new WindowFilterCriteria();
                ApplyMargins();
                ShowNotification($"Applied profile: {profile.Name}", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error applying profile: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void ApplyProfileByIndex(int index)
        {
            var profiles = profileManager.GetProfiles();
            if (index < profiles.Count)
            {
                ApplyProfile(profiles[index]);
            }
        }

        private void SaveCurrentLayoutAsProfile()
        {
            var dialog = new SaveProfileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var profile = new Profile
                {
                    Name = dialog.ProfileName,
                    MarginSettings = new MarginSettings(marginSettings),
                    WindowFilter = new WindowFilterCriteria
                    {
                        ProcessNames = currentFilter.ProcessNames?.ToList(),
                        ExcludedProcesses = currentFilter.ExcludedProcesses?.ToList()
                    }
                };
                
                profileManager.SaveProfile(profile);
                BuildProfilesMenu((ToolStripMenuItem)trayMenu.Items.Cast<ToolStripItem>()
                    .First(i => i.Text == "Profiles"));
                ShowNotification($"Saved profile: {profile.Name}", ToolTipIcon.Info);
            }
        }

        private void ToggleFullscreenMode()
        {
            var windows = windowManager.GetTopLevelWindows(currentFilter);
            foreach (var window in windows)
            {
                if (window.IsMaximized)
                {
                    windowManager.RestoreWindow(window.Handle);
                }
                else
                {
                    windowManager.MaximizeWindow(window.Handle);
                }
            }
        }

        private void ConfigureMonitor(MonitorInfo monitor)
        {
            var configForm = new MonitorConfigForm(monitor, multiMonitorManager);
            configForm.ShowDialog();
        }

        private void MoveWindowsToMonitor(MonitorInfo targetMonitor)
        {
            var windows = windowManager.GetTopLevelWindows(currentFilter);
            foreach (var window in windows)
            {
                var bounds = multiMonitorManager.CalculateOptimalWindowBounds(window.Handle, marginSettings);
                var adjustedBounds = new Rectangle(
                    targetMonitor.WorkingArea.X + (bounds.X % targetMonitor.WorkingArea.Width),
                    targetMonitor.WorkingArea.Y + (bounds.Y % targetMonitor.WorkingArea.Height),
                    Math.Min(bounds.Width, targetMonitor.WorkingArea.Width),
                    Math.Min(bounds.Height, targetMonitor.WorkingArea.Height)
                );
                windowManager.SetWindowPosition(window.Handle, adjustedBounds);
            }
        }

        private void OnMonitorConfigurationChanged(object? sender, MonitorChangedEventArgs e)
        {
            Invoke(() =>
            {
                BuildMonitorsMenu((ToolStripMenuItem)trayMenu.Items.Cast<ToolStripItem>()
                    .First(i => i.Text == "Monitors"));
                ShowNotification($"Monitor configuration changed: {e.NewMonitors.Count} monitors detected", ToolTipIcon.Info);
            });
        }

        private void UpdateTrayIconStatus(object? sender, EventArgs e)
        {
            var status = marginsApplied ? "Margins Applied" : "Ready";
            var windowCount = windowManager.GetTopLevelWindows().Count;
            var monitorCount = multiMonitorManager.GetMonitors().Count;
            
            trayIcon.Text = $"Windows Margin Manager Pro - {status}\n{windowCount} windows, {monitorCount} monitors";
        }

        private void ShowNotification(string message, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(3000, "Windows Margin Manager Pro", message, icon);
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            var settingsForm = new AdvancedSettingsForm(marginSettings, currentFilter, profileManager);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                marginSettings = settingsForm.MarginSettings;
                currentFilter = settingsForm.WindowFilter;
            }
        }

        private void OnManageProfiles(object? sender, EventArgs e)
        {
            var profileForm = new ProfileManagerForm(profileManager);
            if (profileForm.ShowDialog() == DialogResult.OK)
            {
                BuildProfilesMenu((ToolStripMenuItem)trayMenu.Items.Cast<ToolStripItem>()
                    .First(i => i.Text == "Profiles"));
            }
        }

        private void OnScheduling(object? sender, EventArgs e)
        {
            var schedulingForm = new SchedulingForm(schedulingEngine, profileManager);
            schedulingForm.ShowDialog();
        }

        private void OnAbout(object? sender, EventArgs e)
        {
            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            hotkeyManager.UnregisterHotkeys();
            animationEngine.CancelAllAnimations();
            schedulingEngine.Dispose();
            multiMonitorManager.Dispose();
            animationEngine.Dispose();
            statusUpdateTimer.Stop();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hotkeyManager?.UnregisterHotkeys();
                schedulingEngine?.Dispose();
                animationEngine?.Dispose();
                multiMonitorManager?.Dispose();
                statusUpdateTimer?.Dispose();
                trayIcon?.Dispose();
                trayMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
