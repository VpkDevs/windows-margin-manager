using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace WindowsMarginManager
{
    public class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WS_EX_APPWINDOW = 0x00040000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_MINIMIZE = 0x20000000;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private readonly WindowFilter windowFilter;
        private readonly Dictionary<IntPtr, WindowInfo> windowCache;

        public WindowManager()
        {
            windowFilter = new WindowFilter();
            windowCache = new Dictionary<IntPtr, WindowInfo>();
        }

        public List<WindowInfo> GetTopLevelWindows(WindowFilterCriteria? criteria = null)
        {
            var windows = new List<WindowInfo>();
            windowCache.Clear();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsValidWindow(hWnd))
                {
                    var windowInfo = CreateWindowInfo(hWnd);
                    if (windowInfo != null)
                    {
                        windowCache[hWnd] = windowInfo;
                        
                        if (criteria == null || windowFilter.MatchesCriteria(windowInfo, criteria))
                        {
                            windows.Add(windowInfo);
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private WindowInfo? CreateWindowInfo(IntPtr hWnd)
        {
            try
            {
                GetWindowRect(hWnd, out RECT rect);
                var windowRect = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                
                var title = GetWindowTitle(hWnd);
                var className = GetWindowClassName(hWnd);
                var processInfo = GetWindowProcessInfo(hWnd);
                
                var placement = new WINDOWPLACEMENT();
                placement.length = Marshal.SizeOf(placement);
                GetWindowPlacement(hWnd, ref placement);

                return new WindowInfo
                {
                    Handle = hWnd,
                    Rectangle = windowRect,
                    IsMaximized = IsZoomed(hWnd),
                    IsMinimized = IsIconic(hWnd),
                    Title = title,
                    ClassName = className,
                    ProcessName = processInfo.processName,
                    ProcessId = processInfo.processId,
                    WindowState = GetWindowState(hWnd),
                    Placement = placement,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating window info for handle {hWnd}: {ex.Message}");
                return null;
            }
        }

        private string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;
            
            var title = new StringBuilder(length + 1);
            GetWindowText(hWnd, title, title.Capacity);
            return title.ToString();
        }

        private string GetWindowClassName(IntPtr hWnd)
        {
            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }

        private (string processName, uint processId) GetWindowProcessInfo(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                return (process.ProcessName, processId);
            }
            catch
            {
                return ("Unknown", processId);
            }
        }

        private WindowState GetWindowState(IntPtr hWnd)
        {
            if (IsIconic(hWnd)) return WindowState.Minimized;
            if (IsZoomed(hWnd)) return WindowState.Maximized;
            return WindowState.Normal;
        }

        private bool IsValidWindow(IntPtr hWnd)
        {
            if (!IsWindowVisible(hWnd))
                return false;

            if (IsIconic(hWnd))
                return false;

            if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
                return false;

            uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            uint style = GetWindowLong(hWnd, GWL_STYLE);

            if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                return false;

            if ((style & WS_VISIBLE) == 0)
                return false;

            int length = GetWindowTextLength(hWnd);
            if (length == 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                return false;

            return true;
        }

        public void SetWindowPosition(IntPtr hWnd, Rectangle rect)
        {
            if (IsZoomed(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
                System.Threading.Thread.Sleep(50); // Allow time for restore
            }
            
            SetWindowPos(hWnd, IntPtr.Zero, rect.X, rect.Y, rect.Width, rect.Height, 
                SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public void SetWindowPositionSmooth(IntPtr hWnd, Rectangle rect, int durationMs = 300)
        {
            if (IsZoomed(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
                System.Threading.Thread.Sleep(50);
            }

            GetWindowRect(hWnd, out RECT currentRect);
            var startRect = new Rectangle(currentRect.Left, currentRect.Top, 
                currentRect.Right - currentRect.Left, currentRect.Bottom - currentRect.Top);

            var animationEngine = new AnimationEngine();
            _ = animationEngine.AnimateWindowAsync(hWnd, startRect, rect, durationMs);
        }

        public void MaximizeWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_MAXIMIZE);
        }

        public void MinimizeWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_MINIMIZE);
        }

        public void RestoreWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        public void RestoreWindowPlacement(IntPtr hWnd, WINDOWPLACEMENT placement)
        {
            SetWindowPlacement(hWnd, ref placement);
        }

        public Rectangle GetWindowBounds(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public bool IsWindowValid(IntPtr hWnd)
        {
            return IsValidWindow(hWnd);
        }

        public WindowInfo? GetWindowInfo(IntPtr hWnd)
        {
            if (windowCache.TryGetValue(hWnd, out var cachedInfo))
            {
                if (DateTime.UtcNow - cachedInfo.LastUpdated > TimeSpan.FromSeconds(5))
                {
                    var refreshed = CreateWindowInfo(hWnd);
                    if (refreshed != null)
                    {
                        windowCache[hWnd] = refreshed;
                        return refreshed;
                    }
                }
                return cachedInfo;
            }

            var windowInfo = CreateWindowInfo(hWnd);
            if (windowInfo != null)
            {
                windowCache[hWnd] = windowInfo;
            }
            return windowInfo;
        }

        public List<WindowInfo> GetWindowsByProcess(string processName)
        {
            return GetTopLevelWindows().Where(w => 
                w.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<WindowInfo> GetWindowsByTitle(string titlePattern)
        {
            var regex = new Regex(titlePattern, RegexOptions.IgnoreCase);
            return GetTopLevelWindows().Where(w => regex.IsMatch(w.Title)).ToList();
        }

        public void TileWindows(List<WindowInfo> windows, Rectangle bounds, TileLayout layout = TileLayout.Grid)
        {
            if (!windows.Any()) return;

            var positions = CalculateTilePositions(windows.Count, bounds, layout);
            
            for (int i = 0; i < Math.Min(windows.Count, positions.Count); i++)
            {
                SetWindowPosition(windows[i].Handle, positions[i]);
            }
        }

        private List<Rectangle> CalculateTilePositions(int windowCount, Rectangle bounds, TileLayout layout)
        {
            var positions = new List<Rectangle>();
            
            switch (layout)
            {
                case TileLayout.Grid:
                    var cols = (int)Math.Ceiling(Math.Sqrt(windowCount));
                    var rows = (int)Math.Ceiling((double)windowCount / cols);
                    var cellWidth = bounds.Width / cols;
                    var cellHeight = bounds.Height / rows;
                    
                    for (int i = 0; i < windowCount; i++)
                    {
                        var col = i % cols;
                        var row = i / cols;
                        positions.Add(new Rectangle(
                            bounds.X + col * cellWidth,
                            bounds.Y + row * cellHeight,
                            cellWidth,
                            cellHeight
                        ));
                    }
                    break;
                    
                case TileLayout.Horizontal:
                    var windowWidth = bounds.Width / windowCount;
                    for (int i = 0; i < windowCount; i++)
                    {
                        positions.Add(new Rectangle(
                            bounds.X + i * windowWidth,
                            bounds.Y,
                            windowWidth,
                            bounds.Height
                        ));
                    }
                    break;
                    
                case TileLayout.Vertical:
                    var windowHeight = bounds.Height / windowCount;
                    for (int i = 0; i < windowCount; i++)
                    {
                        positions.Add(new Rectangle(
                            bounds.X,
                            bounds.Y + i * windowHeight,
                            bounds.Width,
                            windowHeight
                        ));
                    }
                    break;
            }
            
            return positions;
        }
    }

    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public Rectangle Rectangle { get; set; }
        public bool IsMaximized { get; set; }
        public bool IsMinimized { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public uint ProcessId { get; set; }
        public WindowState WindowState { get; set; }
        public WindowManager.WINDOWPLACEMENT Placement { get; set; }
        public DateTime LastUpdated { get; set; }
        public Rectangle TargetBounds { get; set; }
        public MonitorInfo? AssignedMonitor { get; set; }
    }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    public enum TileLayout
    {
        Grid,
        Horizontal,
        Vertical
    }

    public class WindowFilter
    {
        public bool MatchesCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.TitlePattern))
            {
                var regex = new Regex(criteria.TitlePattern, RegexOptions.IgnoreCase);
                if (!regex.IsMatch(window.Title))
                    return false;
            }

            if (criteria.ProcessNames?.Any() == true)
            {
                if (!criteria.ProcessNames.Any(p => 
                    window.ProcessName.Equals(p, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            if (criteria.MinWidth.HasValue && window.Rectangle.Width < criteria.MinWidth.Value)
                return false;
            if (criteria.MinHeight.HasValue && window.Rectangle.Height < criteria.MinHeight.Value)
                return false;
            if (criteria.MaxWidth.HasValue && window.Rectangle.Width > criteria.MaxWidth.Value)
                return false;
            if (criteria.MaxHeight.HasValue && window.Rectangle.Height > criteria.MaxHeight.Value)
                return false;

            if (criteria.WindowStates?.Any() == true)
            {
                if (!criteria.WindowStates.Contains(window.WindowState))
                    return false;
            }

            if (criteria.ExcludedProcesses?.Any() == true)
            {
                if (criteria.ExcludedProcesses.Any(p => 
                    window.ProcessName.Equals(p, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            if (criteria.ExcludedTitles?.Any() == true)
            {
                if (criteria.ExcludedTitles.Any(t => 
                    window.Title.Contains(t, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }
    }

    public class WindowFilterCriteria
    {
        public string? TitlePattern { get; set; }
        public List<string>? ProcessNames { get; set; }
        public List<string>? ExcludedProcesses { get; set; }
        public List<string>? ExcludedTitles { get; set; }
        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
        public List<WindowState>? WindowStates { get; set; }
        public bool IncludeMinimized { get; set; } = false;
    }
}
