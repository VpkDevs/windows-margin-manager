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
        /// <summary>
        /// Alias for Rectangle property for better API consistency.
        /// </summary>
        public Rectangle Bounds
        {
            get => Rectangle;
            set => Rectangle = value;
        }
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
        private readonly Dictionary<string, Regex> compiledRegexCache = new Dictionary<string, Regex>();
        private readonly Dictionary<string, DateTime> lastMatchCache = new Dictionary<string, DateTime>();

        public bool MatchesCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria == null) return true;

            if (!CheckBasicCriteria(window, criteria)) return false;
            if (!CheckSizeCriteria(window, criteria)) return false;
            if (!CheckStateCriteria(window, criteria)) return false;
            if (!CheckProcessCriteria(window, criteria)) return false;
            if (!CheckTitleCriteria(window, criteria)) return false;
            if (!CheckClassNameCriteria(window, criteria)) return false;
            if (!CheckPositionCriteria(window, criteria)) return false;
            if (!CheckTimeCriteria(window, criteria)) return false;
            if (!CheckMonitorCriteria(window, criteria)) return false;
            if (!CheckCustomCriteria(window, criteria)) return false;

            UpdateMatchCache(window, criteria);
            return true;
        }

        private bool CheckBasicCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (!criteria.IncludeMinimized && window.IsMinimized)
                return false;

            if (criteria.RequireVisibleTitle && string.IsNullOrWhiteSpace(window.Title))
                return false;

            if (criteria.MaxAge.HasValue)
            {
                var windowAge = DateTime.UtcNow - window.LastUpdated;
                if (windowAge > criteria.MaxAge.Value)
                    return false;
            }

            return true;
        }

        private bool CheckSizeCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            var rect = window.Rectangle;

            if (criteria.MinWidth.HasValue && rect.Width < criteria.MinWidth.Value) return false;
            if (criteria.MinHeight.HasValue && rect.Height < criteria.MinHeight.Value) return false;
            if (criteria.MaxWidth.HasValue && rect.Width > criteria.MaxWidth.Value) return false;
            if (criteria.MaxHeight.HasValue && rect.Height > criteria.MaxHeight.Value) return false;

            if (criteria.MinArea.HasValue && (rect.Width * rect.Height) < criteria.MinArea.Value) return false;
            if (criteria.MaxArea.HasValue && (rect.Width * rect.Height) > criteria.MaxArea.Value) return false;

            if (criteria.AspectRatioRange.HasValue)
            {
                var aspectRatio = (double)rect.Width / rect.Height;
                if (aspectRatio < criteria.AspectRatioRange.Value.Min || aspectRatio > criteria.AspectRatioRange.Value.Max)
                    return false;
            }

            return true;
        }

        private bool CheckStateCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.WindowStates?.Any() == true)
            {
                if (!criteria.WindowStates.Contains(window.WindowState))
                    return false;
            }

            if (criteria.ExcludedStates?.Any() == true)
            {
                if (criteria.ExcludedStates.Contains(window.WindowState))
                    return false;
            }

            return true;
        }

        private bool CheckProcessCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.ProcessNames?.Any() == true)
            {
                var processMatches = criteria.ProcessNames.Any(p => 
                    MatchesPattern(window.ProcessName, p, criteria.CaseSensitive));
                if (!processMatches) return false;
            }

            if (criteria.ExcludedProcesses?.Any() == true)
            {
                var processExcluded = criteria.ExcludedProcesses.Any(p => 
                    MatchesPattern(window.ProcessName, p, criteria.CaseSensitive));
                if (processExcluded) return false;
            }

            if (criteria.ProcessIdRange.HasValue)
            {
                if (window.ProcessId < criteria.ProcessIdRange.Value.Min || window.ProcessId > criteria.ProcessIdRange.Value.Max)
                    return false;
            }

            return true;
        }

        private bool CheckTitleCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.TitlePattern))
            {
                if (!MatchesRegexPattern(window.Title, criteria.TitlePattern, criteria.CaseSensitive))
                    return false;
            }

            if (criteria.TitleContains?.Any() == true)
            {
                var titleMatches = criteria.TitleContains.Any(t => 
                    window.Title.Contains(t, criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                if (!titleMatches) return false;
            }

            if (criteria.ExcludedTitles?.Any() == true)
            {
                var titleExcluded = criteria.ExcludedTitles.Any(t => 
                    window.Title.Contains(t, criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                if (titleExcluded) return false;
            }

            if (criteria.TitleStartsWith?.Any() == true)
            {
                var startsWithMatch = criteria.TitleStartsWith.Any(t => 
                    window.Title.StartsWith(t, criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                if (!startsWithMatch) return false;
            }

            if (criteria.TitleEndsWith?.Any() == true)
            {
                var endsWithMatch = criteria.TitleEndsWith.Any(t => 
                    window.Title.EndsWith(t, criteria.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                if (!endsWithMatch) return false;
            }

            return true;
        }

        private bool CheckClassNameCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.ClassNamePattern))
            {
                if (!MatchesRegexPattern(window.ClassName, criteria.ClassNamePattern, criteria.CaseSensitive))
                    return false;
            }

            if (criteria.ClassNames?.Any() == true)
            {
                var classMatches = criteria.ClassNames.Any(c => 
                    MatchesPattern(window.ClassName, c, criteria.CaseSensitive));
                if (!classMatches) return false;
            }

            if (criteria.ExcludedClassNames?.Any() == true)
            {
                var classExcluded = criteria.ExcludedClassNames.Any(c => 
                    MatchesPattern(window.ClassName, c, criteria.CaseSensitive));
                if (classExcluded) return false;
            }

            return true;
        }

        private bool CheckPositionCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.PositionBounds != null)
            {
                var bounds = criteria.PositionBounds.Value;
                var windowCenter = new Point(
                    window.Rectangle.X + window.Rectangle.Width / 2,
                    window.Rectangle.Y + window.Rectangle.Height / 2
                );

                if (!bounds.Contains(windowCenter))
                    return false;
            }

            if (criteria.ExcludedPositionBounds?.Any() == true)
            {
                var windowCenter = new Point(
                    window.Rectangle.X + window.Rectangle.Width / 2,
                    window.Rectangle.Y + window.Rectangle.Height / 2
                );

                if (criteria.ExcludedPositionBounds.Any(bounds => bounds.Contains(windowCenter)))
                    return false;
            }

            return true;
        }

        private bool CheckTimeCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.TimeRange.HasValue)
            {
                var currentTime = DateTime.Now.TimeOfDay;
                if (currentTime < criteria.TimeRange.Value.Start || currentTime > criteria.TimeRange.Value.End)
                    return false;
            }

            if (criteria.DaysOfWeek?.Any() == true)
            {
                if (!criteria.DaysOfWeek.Contains(DateTime.Now.DayOfWeek))
                    return false;
            }

            return true;
        }

        private bool CheckMonitorCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.MonitorIndices?.Any() == true)
            {
                var monitor = window.AssignedMonitor;
                if (monitor == null) return false;

                var monitorIndex = GetMonitorIndex(monitor);
                if (!criteria.MonitorIndices.Contains(monitorIndex))
                    return false;
            }

            if (criteria.PrimaryMonitorOnly && window.AssignedMonitor?.IsPrimary != true)
                return false;

            if (criteria.ExcludePrimaryMonitor && window.AssignedMonitor?.IsPrimary == true)
                return false;

            return true;
        }

        private bool CheckCustomCriteria(WindowInfo window, WindowFilterCriteria criteria)
        {
            if (criteria.CustomFilters?.Any() == true)
            {
                foreach (var customFilter in criteria.CustomFilters)
                {
                    if (!customFilter.Invoke(window))
                        return false;
                }
            }

            return true;
        }

        private bool MatchesPattern(string text, string pattern, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(pattern)) return true;
            if (string.IsNullOrEmpty(text)) return false;

            if (pattern.Contains("*") || pattern.Contains("?"))
            {
                return MatchesWildcardPattern(text, pattern, caseSensitive);
            }

            return text.Equals(pattern, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesWildcardPattern(string text, string pattern, bool caseSensitive)
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            
            return GetCachedRegex(regexPattern, options).IsMatch(text);
        }

        private bool MatchesRegexPattern(string text, string pattern, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(pattern)) return true;
            if (string.IsNullOrEmpty(text)) return false;

            try
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                return GetCachedRegex(pattern, options).IsMatch(text);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private Regex GetCachedRegex(string pattern, RegexOptions options)
        {
            var key = $"{pattern}|{options}";
            if (!compiledRegexCache.TryGetValue(key, out var regex))
            {
                regex = new Regex(pattern, options | RegexOptions.Compiled);
                compiledRegexCache[key] = regex;
            }
            return regex;
        }

        private int GetMonitorIndex(MonitorInfo monitor)
        {
            return monitor.IsPrimary ? 0 : monitor.Handle.ToInt32() % 10;
        }

        private void UpdateMatchCache(WindowInfo window, WindowFilterCriteria criteria)
        {
            var key = $"{window.Handle}|{criteria.GetHashCode()}";
            lastMatchCache[key] = DateTime.UtcNow;
        }

        public void ClearCache()
        {
            compiledRegexCache.Clear();
            lastMatchCache.Clear();
        }

        public FilterStatistics GetStatistics()
        {
            return new FilterStatistics
            {
                CachedRegexCount = compiledRegexCache.Count,
                CachedMatchCount = lastMatchCache.Count,
                LastCacheUpdate = lastMatchCache.Values.DefaultIfEmpty(DateTime.MinValue).Max()
            };
        }
    }

    public class WindowFilterCriteria
    {
        public string? TitlePattern { get; set; }
        public List<string>? TitleContains { get; set; }
        public List<string>? TitleStartsWith { get; set; }
        public List<string>? TitleEndsWith { get; set; }
        public List<string>? ExcludedTitles { get; set; }
        
        public string? ClassNamePattern { get; set; }
        public List<string>? ClassNames { get; set; }
        public List<string>? ExcludedClassNames { get; set; }
        
        public List<string>? ProcessNames { get; set; }
        public List<string>? ExcludedProcesses { get; set; }
        public Range<uint>? ProcessIdRange { get; set; }
        
        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
        public int? MinArea { get; set; }
        public int? MaxArea { get; set; }
        public Range<double>? AspectRatioRange { get; set; }
        
        public List<WindowState>? WindowStates { get; set; }
        public List<WindowState>? ExcludedStates { get; set; }
        public bool IncludeMinimized { get; set; } = false;
        public bool RequireVisibleTitle { get; set; } = true;
        
        public Rectangle? PositionBounds { get; set; }
        public List<Rectangle>? ExcludedPositionBounds { get; set; }
        
        public List<int>? MonitorIndices { get; set; }
        public bool PrimaryMonitorOnly { get; set; } = false;
        public bool ExcludePrimaryMonitor { get; set; } = false;
        
        public TimeRange? TimeRange { get; set; }
        public List<DayOfWeek>? DaysOfWeek { get; set; }
        public TimeSpan? MaxAge { get; set; }
        
        public bool CaseSensitive { get; set; } = false;
        public FilterMode Mode { get; set; } = FilterMode.Include;
        public int Priority { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        
        public List<Func<WindowInfo, bool>>? CustomFilters { get; set; }
        public Dictionary<string, object>? CustomProperties { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public WindowFilterCriteria Clone()
        {
            return new WindowFilterCriteria
            {
                TitlePattern = TitlePattern,
                TitleContains = TitleContains?.ToList(),
                TitleStartsWith = TitleStartsWith?.ToList(),
                TitleEndsWith = TitleEndsWith?.ToList(),
                ExcludedTitles = ExcludedTitles?.ToList(),
                ClassNamePattern = ClassNamePattern,
                ClassNames = ClassNames?.ToList(),
                ExcludedClassNames = ExcludedClassNames?.ToList(),
                ProcessNames = ProcessNames?.ToList(),
                ExcludedProcesses = ExcludedProcesses?.ToList(),
                ProcessIdRange = ProcessIdRange,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                MaxWidth = MaxWidth,
                MaxHeight = MaxHeight,
                MinArea = MinArea,
                MaxArea = MaxArea,
                AspectRatioRange = AspectRatioRange,
                WindowStates = WindowStates?.ToList(),
                ExcludedStates = ExcludedStates?.ToList(),
                IncludeMinimized = IncludeMinimized,
                RequireVisibleTitle = RequireVisibleTitle,
                PositionBounds = PositionBounds,
                ExcludedPositionBounds = ExcludedPositionBounds?.ToList(),
                MonitorIndices = MonitorIndices?.ToList(),
                PrimaryMonitorOnly = PrimaryMonitorOnly,
                ExcludePrimaryMonitor = ExcludePrimaryMonitor,
                TimeRange = TimeRange,
                DaysOfWeek = DaysOfWeek?.ToList(),
                MaxAge = MaxAge,
                CaseSensitive = CaseSensitive,
                Mode = Mode,
                Priority = Priority,
                Enabled = Enabled,
                CustomFilters = CustomFilters?.ToList(),
                CustomProperties = CustomProperties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Name = Name,
                Description = Description,
                Created = Created,
                LastModified = DateTime.UtcNow
            };
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(TitlePattern);
            hash.Add(ClassNamePattern);
            hash.Add(ProcessNames?.Count ?? 0);
            hash.Add(MinWidth);
            hash.Add(MinHeight);
            hash.Add(MaxWidth);
            hash.Add(MaxHeight);
            hash.Add(WindowStates?.Count ?? 0);
            hash.Add(IncludeMinimized);
            hash.Add(CaseSensitive);
            hash.Add(Mode);
            hash.Add(Enabled);
            return hash.ToHashCode();
        }
    }

    public struct Range<T> where T : IComparable<T>
    {
        public T Min { get; set; }
        public T Max { get; set; }

        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(T value)
        {
            return value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0;
        }
    }

    public struct TimeRange
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }

        public TimeRange(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }

        public bool Contains(TimeSpan time)
        {
            if (Start <= End)
            {
                return time >= Start && time <= End;
            }
            else
            {
                return time >= Start || time <= End;
            }
        }
    }

    public enum FilterMode
    {
        Include,
        Exclude,
        Highlight,
        Priority
    }

    public class FilterStatistics
    {
        public int CachedRegexCount { get; set; }
        public int CachedMatchCount { get; set; }
        public DateTime LastCacheUpdate { get; set; }
        public int TotalFiltersApplied { get; set; }
        public int WindowsMatched { get; set; }
        public int WindowsExcluded { get; set; }
        public TimeSpan AverageFilterTime { get; set; }
    }
}
