using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using WindowsDisplayAPI;
using WindowsDisplayAPI.DisplayConfig;

namespace WindowsMarginManager
{
    public class MultiMonitorManager
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint MONITORINFOF_PRIMARY = 1;

        private List<MonitorInfo> monitors;
        private Dictionary<IntPtr, MonitorProfile> monitorProfiles;

        public event EventHandler<MonitorChangedEventArgs>? MonitorConfigurationChanged;

        public MultiMonitorManager()
        {
            monitors = new List<MonitorInfo>();
            monitorProfiles = new Dictionary<IntPtr, MonitorProfile>();
            RefreshMonitors();
            
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public void RefreshMonitors()
        {
            monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
            
            foreach (var screen in Screen.AllScreens)
            {
                var monitor = monitors.FirstOrDefault(m => m.Bounds == screen.Bounds);
                if (monitor != null)
                {
                    monitor.Screen = screen;
                    monitor.IsPrimary = screen.Primary;
                    monitor.DeviceName = screen.DeviceName;
                    monitor.WorkingArea = screen.WorkingArea;
                }
            }

            DetectMonitorCapabilities();
        }

        private bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf(mi);
            
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var monitor = new MonitorInfo
                {
                    Handle = hMonitor,
                    Bounds = new Rectangle(mi.rcMonitor.Left, mi.rcMonitor.Top, 
                        mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                    WorkingArea = new Rectangle(mi.rcWork.Left, mi.rcWork.Top,
                        mi.rcWork.Right - mi.rcWork.Left, mi.rcWork.Bottom - mi.rcWork.Top),
                    IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
                    DeviceName = mi.szDevice
                };
                
                monitors.Add(monitor);
            }
            
            return true;
        }

        private void DetectMonitorCapabilities()
        {
            try
            {
                var displays = Display.GetDisplays();
                foreach (var display in displays)
                {
                    var monitor = monitors.FirstOrDefault(m => m.DeviceName.Contains(display.DeviceName));
                    if (monitor != null)
                    {
                        monitor.IsUltrawide = (double)monitor.Bounds.Width / monitor.Bounds.Height > 2.0;
                        monitor.IsCurved = DetectCurvedDisplay(display);
                        monitor.RefreshRate = (int)display.CurrentSetting.Frequency;
                        monitor.ColorDepth = (int)display.CurrentSetting.ColorDepth;
                        monitor.DpiScaling = GetDpiScaling(monitor.Handle);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting monitor capabilities: {ex.Message}");
            }
        }

        private bool DetectCurvedDisplay(Display display)
        {
            return display.DisplayName.ToLower().Contains("curved") || 
                   display.DisplayName.ToLower().Contains("c27") ||
                   display.DisplayName.ToLower().Contains("c32");
        }

        private double GetDpiScaling(IntPtr hMonitor)
        {
            try
            {
                var dpi = GetDpiForMonitor(hMonitor);
                return dpi / 96.0; // 96 DPI is 100% scaling
            }
            catch
            {
                return 1.0; // Default to 100% scaling
            }
        }

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        private int GetDpiForMonitor(IntPtr hMonitor)
        {
            if (GetDpiForMonitor(hMonitor, 0, out uint dpiX, out uint dpiY) == 0)
            {
                return (int)dpiX;
            }
            return 96; // Default DPI
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            var oldMonitors = new List<MonitorInfo>(monitors);
            RefreshMonitors();
            
            MonitorConfigurationChanged?.Invoke(this, new MonitorChangedEventArgs
            {
                OldMonitors = oldMonitors,
                NewMonitors = new List<MonitorInfo>(monitors)
            });
        }

        public List<MonitorInfo> GetMonitors() => new List<MonitorInfo>(monitors);

        public MonitorInfo? GetPrimaryMonitor() => monitors.FirstOrDefault(m => m.IsPrimary);

        public MonitorInfo? GetMonitorFromWindow(IntPtr windowHandle)
        {
            var hMonitor = MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);
            return monitors.FirstOrDefault(m => m.Handle == hMonitor);
        }

        public MonitorInfo? GetMonitorFromPoint(Point point)
        {
            var pt = new POINT { X = point.X, Y = point.Y };
            var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return monitors.FirstOrDefault(m => m.Handle == hMonitor);
        }

        public void SetMonitorProfile(IntPtr monitorHandle, MonitorProfile profile)
        {
            monitorProfiles[monitorHandle] = profile;
        }

        public MonitorProfile? GetMonitorProfile(IntPtr monitorHandle)
        {
            return monitorProfiles.TryGetValue(monitorHandle, out var profile) ? profile : null;
        }

        public Rectangle CalculateOptimalWindowBounds(IntPtr windowHandle, MarginSettings globalSettings)
        {
            var monitor = GetMonitorFromWindow(windowHandle);
            if (monitor == null) return Rectangle.Empty;

            var profile = GetMonitorProfile(monitor.Handle);
            var settings = profile?.MarginSettings ?? globalSettings;

            var workingArea = monitor.WorkingArea;
            
            var scaleFactor = monitor.DpiScaling;
            
            int leftMargin, topMargin, rightMargin, bottomMargin;
            
            if (settings.UsePercentage)
            {
                leftMargin = (int)(workingArea.Width * settings.LeftMargin / 100 * scaleFactor);
                topMargin = (int)(workingArea.Height * settings.TopMargin / 100 * scaleFactor);
                rightMargin = (int)(workingArea.Width * settings.RightMargin / 100 * scaleFactor);
                bottomMargin = (int)(workingArea.Height * settings.BottomMargin / 100 * scaleFactor);
            }
            else
            {
                leftMargin = (int)(settings.LeftMargin * scaleFactor);
                topMargin = (int)(settings.TopMargin * scaleFactor);
                rightMargin = (int)(settings.RightMargin * scaleFactor);
                bottomMargin = (int)(settings.BottomMargin * scaleFactor);
            }

            if (monitor.IsUltrawide)
            {
                leftMargin = Math.Max(leftMargin, (int)(workingArea.Width * 0.1)); // Minimum 10% margin for ultrawide
                rightMargin = Math.Max(rightMargin, (int)(workingArea.Width * 0.1));
            }

            return new Rectangle(
                workingArea.Left + leftMargin,
                workingArea.Top + topMargin,
                workingArea.Width - leftMargin - rightMargin,
                workingArea.Height - topMargin - bottomMargin
            );
        }

        public void DistributeWindowsAcrossMonitors(List<WindowInfo> windows)
        {
            if (monitors.Count <= 1) return;

            var availableMonitors = monitors.Where(m => !m.IsPrimary || monitors.Count == 1).ToList();
            if (!availableMonitors.Any()) availableMonitors = monitors;

            var windowsPerMonitor = Math.Max(1, windows.Count / availableMonitors.Count);
            
            for (int i = 0; i < windows.Count; i++)
            {
                var monitorIndex = Math.Min(i / windowsPerMonitor, availableMonitors.Count - 1);
                var targetMonitor = availableMonitors[monitorIndex];
                
                var optimalBounds = CalculateOptimalWindowBounds(windows[i].Handle, new MarginSettings());
                if (optimalBounds != Rectangle.Empty)
                {
                    var adjustedBounds = new Rectangle(
                        targetMonitor.WorkingArea.X + (optimalBounds.X - monitors[0].WorkingArea.X),
                        targetMonitor.WorkingArea.Y + (optimalBounds.Y - monitors[0].WorkingArea.Y),
                        Math.Min(optimalBounds.Width, targetMonitor.WorkingArea.Width),
                        Math.Min(optimalBounds.Height, targetMonitor.WorkingArea.Height)
                    );
                    
                    windows[i].TargetBounds = adjustedBounds;
                }
            }
        }

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }

    public class MonitorInfo
    {
        public IntPtr Handle { get; set; }
        public Rectangle Bounds { get; set; }
        public Rectangle WorkingArea { get; set; }
        public bool IsPrimary { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public Screen? Screen { get; set; }
        public bool IsUltrawide { get; set; }
        public bool IsCurved { get; set; }
        public int RefreshRate { get; set; }
        public int ColorDepth { get; set; }
        public double DpiScaling { get; set; } = 1.0;
        /// <summary>
        /// Alias for DpiScaling property for API consistency.
        /// </summary>
        public double DpiScale
        {
            get => DpiScaling;
            set => DpiScaling = value;
        }
    }

    public class MonitorProfile
    {
        public string Name { get; set; } = string.Empty;
        public MarginSettings MarginSettings { get; set; } = new MarginSettings();
        public bool EnableAnimations { get; set; } = true;
        public int AnimationDuration { get; set; } = 300;
        public List<string> ExcludedApplications { get; set; } = new List<string>();
    }

    public class MonitorChangedEventArgs : EventArgs
    {
        public List<MonitorInfo> OldMonitors { get; set; } = new List<MonitorInfo>();
        public List<MonitorInfo> NewMonitors { get; set; } = new List<MonitorInfo>();
    }
}
