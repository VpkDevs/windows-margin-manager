using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsMarginManager
{
    public class SystemEventMonitor : IDisposable
    {
        private ManagementEventWatcher? processStartWatcher;
        private ManagementEventWatcher? processStopWatcher;
        private bool disposed = false;

        public event EventHandler<ApplicationEventArgs>? ApplicationLaunched;
        public event EventHandler<ApplicationEventArgs>? ApplicationClosed;
        public event EventHandler? MonitorConfigurationChanged;
        public event EventHandler? SystemResumed;
        public event EventHandler? UserLoggedIn;
        public event EventHandler? SystemLocked;
        public event EventHandler? SystemUnlocked;

        public SystemEventMonitor()
        {
            SetupProcessMonitoring();
            SetupSystemEventMonitoring();
        }

        private void SetupProcessMonitoring()
        {
            try
            {
                var processStartQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                processStartWatcher = new ManagementEventWatcher(processStartQuery);
                processStartWatcher.EventArrived += OnProcessStarted;
                processStartWatcher.Start();

                var processStopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
                processStopWatcher = new ManagementEventWatcher(processStopQuery);
                processStopWatcher.EventArrived += OnProcessStopped;
                processStopWatcher.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up process monitoring: {ex.Message}");
            }
        }

        private void SetupSystemEventMonitoring()
        {
            try
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
                SystemEvents.SessionSwitch += OnSessionSwitch;
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up system event monitoring: {ex.Message}");
            }
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var processName = e.NewEvent["ProcessName"]?.ToString();
                var processId = Convert.ToInt32(e.NewEvent["ProcessID"]);
                
                if (!string.IsNullOrEmpty(processName))
                {
                    ApplicationLaunched?.Invoke(this, new ApplicationEventArgs
                    {
                        ProcessName = processName,
                        ProcessId = processId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing application launch event: {ex.Message}");
            }
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var processName = e.NewEvent["ProcessName"]?.ToString();
                var processId = Convert.ToInt32(e.NewEvent["ProcessID"]);
                
                if (!string.IsNullOrEmpty(processName))
                {
                    ApplicationClosed?.Invoke(this, new ApplicationEventArgs
                    {
                        ProcessName = processName,
                        ProcessId = processId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing application close event: {ex.Message}");
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            MonitorConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                SystemResumed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLogon:
                    UserLoggedIn?.Invoke(this, EventArgs.Empty);
                    break;
                case SessionSwitchReason.SessionLock:
                    SystemLocked?.Invoke(this, EventArgs.Empty);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    SystemUnlocked?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Desktop)
            {
                MonitorConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    processStartWatcher?.Stop();
                    processStartWatcher?.Dispose();
                    processStopWatcher?.Stop();
                    processStopWatcher?.Dispose();

                    SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                    SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                    SystemEvents.SessionSwitch -= OnSessionSwitch;
                    SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing SystemEventMonitor: {ex.Message}");
                }
                
                disposed = true;
            }
        }
    }

    public class ApplicationEventArgs : EventArgs
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
