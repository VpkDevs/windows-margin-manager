using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class UsageAnalyzer : IDisposable
    {
        private readonly string dataPath;
        private Dictionary<string, WindowUsageHistory> usageData;
        private readonly object lockObject = new object();
        private bool disposed = false;

        public UsageAnalyzer(string dataPath)
        {
            this.dataPath = dataPath;
            usageData = new Dictionary<string, WindowUsageHistory>();
            LoadUsageData();
        }

        public void RecordWindowPlacement(string processName, Rectangle bounds, MonitorInfo monitor)
        {
            lock (lockObject)
            {
                if (!usageData.ContainsKey(processName))
                {
                    usageData[processName] = new WindowUsageHistory { ProcessName = processName };
                }

                var history = usageData[processName];
                var placement = new WindowPlacement
                {
                    Bounds = bounds,
                    MonitorId = monitor.DeviceName,
                    Timestamp = DateTime.UtcNow,
                    ScreenResolution = new Size(monitor.Bounds.Width, monitor.Bounds.Height)
                };

                history.Placements.Add(placement);
                history.LastUsed = DateTime.UtcNow;
                history.UsageCount++;

                if (history.Placements.Count > 100)
                {
                    history.Placements.RemoveAt(0);
                }

                SaveUsageData();
            }
        }

        public void RecordUserFeedback(string processName, Rectangle bounds, UserFeedback feedback)
        {
            lock (lockObject)
            {
                if (!usageData.ContainsKey(processName))
                {
                    usageData[processName] = new WindowUsageHistory { ProcessName = processName };
                }

                var history = usageData[processName];
                var feedbackEntry = new UserFeedbackEntry
                {
                    Bounds = bounds,
                    Feedback = feedback,
                    Timestamp = DateTime.UtcNow
                };

                history.UserFeedback.Add(feedbackEntry);

                if (history.UserFeedback.Count > 50)
                {
                    history.UserFeedback.RemoveAt(0);
                }

                SaveUsageData();
            }
        }

        public WindowUsageHistory GetWindowUsageHistory(string processName)
        {
            lock (lockObject)
            {
                return usageData.ContainsKey(processName) 
                    ? usageData[processName] 
                    : new WindowUsageHistory { ProcessName = processName };
            }
        }

        public Rectangle? PredictOptimalPosition(string processName, MonitorInfo monitor)
        {
            var history = GetWindowUsageHistory(processName);
            if (!history.Placements.Any())
                return null;

            var relevantPlacements = history.Placements
                .Where(p => p.MonitorId == monitor.DeviceName && 
                           p.ScreenResolution.Width == monitor.Bounds.Width &&
                           p.ScreenResolution.Height == monitor.Bounds.Height)
                .ToList();

            if (!relevantPlacements.Any())
            {
                relevantPlacements = history.Placements.ToList();
            }

            if (!relevantPlacements.Any())
                return null;

            var weightedPlacements = relevantPlacements
                .Select(p => new
                {
                    Placement = p,
                    Weight = CalculateTimeWeight(p.Timestamp)
                })
                .ToList();

            var totalWeight = weightedPlacements.Sum(wp => wp.Weight);
            if (totalWeight == 0)
                return null;

            var avgX = (int)weightedPlacements.Sum(wp => wp.Placement.Bounds.X * wp.Weight) / totalWeight;
            var avgY = (int)weightedPlacements.Sum(wp => wp.Placement.Bounds.Y * wp.Weight) / totalWeight;
            var avgWidth = (int)weightedPlacements.Sum(wp => wp.Placement.Bounds.Width * wp.Weight) / totalWeight;
            var avgHeight = (int)weightedPlacements.Sum(wp => wp.Placement.Bounds.Height * wp.Weight) / totalWeight;

            return new Rectangle(avgX, avgY, avgWidth, avgHeight);
        }

        public List<Rectangle> GetFrequentPositions(string processName, MonitorInfo monitor, int maxResults = 5)
        {
            var history = GetWindowUsageHistory(processName);
            if (!history.Placements.Any())
                return new List<Rectangle>();

            var positionGroups = new List<List<WindowPlacement>>();
            
            foreach (var placement in history.Placements)
            {
                var foundGroup = false;
                foreach (var group in positionGroups)
                {
                    var representative = group.First();
                    if (Math.Abs(representative.Bounds.X - placement.Bounds.X) <= 50 &&
                        Math.Abs(representative.Bounds.Y - placement.Bounds.Y) <= 50)
                    {
                        group.Add(placement);
                        foundGroup = true;
                        break;
                    }
                }

                if (!foundGroup)
                {
                    positionGroups.Add(new List<WindowPlacement> { placement });
                }
            }

            return positionGroups
                .OrderByDescending(g => g.Count)
                .Take(maxResults)
                .Select(g => CalculateAveragePosition(g))
                .ToList();
        }

        public UsageStatistics GetUsageStatistics()
        {
            lock (lockObject)
            {
                var stats = new UsageStatistics
                {
                    TotalApplications = usageData.Count,
                    TotalPlacements = usageData.Values.Sum(h => h.Placements.Count),
                    TotalFeedbackEntries = usageData.Values.Sum(h => h.UserFeedback.Count),
                    MostUsedApplications = usageData.Values
                        .OrderByDescending(h => h.UsageCount)
                        .Take(10)
                        .Select(h => new ApplicationUsage
                        {
                            ProcessName = h.ProcessName,
                            UsageCount = h.UsageCount,
                            LastUsed = h.LastUsed,
                            AveragePositionCount = h.Placements.Count
                        })
                        .ToList(),
                    FeedbackDistribution = usageData.Values
                        .SelectMany(h => h.UserFeedback)
                        .GroupBy(f => f.Feedback)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return stats;
            }
        }

        private double CalculateTimeWeight(DateTime timestamp)
        {
            var daysSince = (DateTime.UtcNow - timestamp).TotalDays;
            return Math.Exp(-daysSince / 30.0); // 30-day half-life
        }

        private Rectangle CalculateAveragePosition(List<WindowPlacement> placements)
        {
            var avgX = (int)placements.Average(p => p.Bounds.X);
            var avgY = (int)placements.Average(p => p.Bounds.Y);
            var avgWidth = (int)placements.Average(p => p.Bounds.Width);
            var avgHeight = (int)placements.Average(p => p.Bounds.Height);

            return new Rectangle(avgX, avgY, avgWidth, avgHeight);
        }

        private void LoadUsageData()
        {
            try
            {
                if (File.Exists(dataPath))
                {
                    var json = File.ReadAllText(dataPath);
                    var loadedData = JsonConvert.DeserializeObject<Dictionary<string, WindowUsageHistory>>(json);
                    if (loadedData != null)
                    {
                        usageData = loadedData;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading usage data: {ex.Message}");
            }
        }

        private void SaveUsageData()
        {
            try
            {
                var directory = Path.GetDirectoryName(dataPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(usageData, Formatting.Indented);
                File.WriteAllText(dataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving usage data: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                SaveUsageData();
                disposed = true;
            }
        }
    }

    public class WindowUsageHistory
    {
        public string ProcessName { get; set; } = string.Empty;
        public List<WindowPlacement> Placements { get; set; } = new List<WindowPlacement>();
        public List<UserFeedbackEntry> UserFeedback { get; set; } = new List<UserFeedbackEntry>();
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public int UsageCount { get; set; } = 0;
    }

    public class WindowPlacement
    {
        public Rectangle Bounds { get; set; }
        public string MonitorId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Size ScreenResolution { get; set; }
    }

    public class UserFeedbackEntry
    {
        public Rectangle Bounds { get; set; }
        public UserFeedback Feedback { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UsageStatistics
    {
        public int TotalApplications { get; set; }
        public int TotalPlacements { get; set; }
        public int TotalFeedbackEntries { get; set; }
        public List<ApplicationUsage> MostUsedApplications { get; set; } = new List<ApplicationUsage>();
        public Dictionary<UserFeedback, int> FeedbackDistribution { get; set; } = new Dictionary<UserFeedback, int>();
    }

    public class ApplicationUsage
    {
        public string ProcessName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public int AveragePositionCount { get; set; }
    }
}
