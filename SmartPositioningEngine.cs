using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class SmartPositioningEngine : IDisposable
    {
        private readonly string usageDataPath;
        private readonly WindowManager windowManager;
        private readonly MultiMonitorManager multiMonitorManager;
        private UsageAnalyzer usageAnalyzer;
        private CollisionDetector collisionDetector;
        private AdaptiveMarginCalculator marginCalculator;
        private GoldenRatioCalculator goldenRatioCalculator;
        private bool disposed = false;

        public SmartPositioningEngine(WindowManager windowManager, MultiMonitorManager multiMonitorManager)
        {
            this.windowManager = windowManager;
            this.multiMonitorManager = multiMonitorManager;
            
            usageDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowsMarginManager",
                "usage_data.json"
            );

            usageAnalyzer = new UsageAnalyzer(usageDataPath);
            collisionDetector = new CollisionDetector();
            marginCalculator = new AdaptiveMarginCalculator();
            goldenRatioCalculator = new GoldenRatioCalculator();
        }

        public SmartPositionResult CalculateOptimalPosition(IntPtr windowHandle, SmartPositionOptions options)
        {
            var windowInfo = windowManager.GetWindowInfo(windowHandle);
            if (windowInfo == null)
            {
                return new SmartPositionResult { Success = false, Error = "Window not found" };
            }

            var monitor = multiMonitorManager.GetMonitorFromWindow(windowHandle);
            if (monitor == null)
            {
                return new SmartPositionResult { Success = false, Error = "Monitor not found" };
            }

            var context = new PositioningContext
            {
                WindowInfo = windowInfo,
                Monitor = monitor,
                Options = options,
                ExistingWindows = windowManager.GetTopLevelWindows(new WindowFilterCriteria()),
                UsageHistory = usageAnalyzer.GetWindowUsageHistory(windowInfo.ProcessName)
            };

            var algorithms = GetPositioningAlgorithms(options);
            var candidates = new List<PositionCandidate>();

            foreach (var algorithm in algorithms)
            {
                var candidate = algorithm.CalculatePosition(context);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }

            if (!candidates.Any())
            {
                return new SmartPositionResult { Success = false, Error = "No valid positions found" };
            }

            var bestCandidate = SelectBestCandidate(candidates, context);
            
            usageAnalyzer.RecordWindowPlacement(windowInfo.ProcessName, bestCandidate.Bounds, monitor);

            return new SmartPositionResult
            {
                Success = true,
                RecommendedBounds = bestCandidate.Bounds,
                Algorithm = bestCandidate.Algorithm,
                Confidence = bestCandidate.Score,
                Reasoning = bestCandidate.Reasoning,
                AlternativePositions = candidates.Where(c => c != bestCandidate)
                    .OrderByDescending(c => c.Score)
                    .Take(3)
                    .Select(c => new AlternativePosition
                    {
                        Bounds = c.Bounds,
                        Algorithm = c.Algorithm,
                        Score = c.Score,
                        Description = c.Reasoning
                    }).ToList()
            };
        }

        private List<IPositioningAlgorithm> GetPositioningAlgorithms(SmartPositionOptions options)
        {
            var algorithms = new List<IPositioningAlgorithm>();

            if (options.UseUsagePatterns)
                algorithms.Add(new UsagePatternAlgorithm(usageAnalyzer));
            
            if (options.UseGoldenRatio)
                algorithms.Add(new GoldenRatioAlgorithm(goldenRatioCalculator));
            
            if (options.UseContentAware)
                algorithms.Add(new ContentAwareAlgorithm());
            
            if (options.UseCollisionAvoidance)
                algorithms.Add(new CollisionAvoidanceAlgorithm(collisionDetector));
            
            algorithms.Add(new AdaptiveMarginAlgorithm(marginCalculator));
            algorithms.Add(new ScreenZoneAlgorithm());
            algorithms.Add(new WorkflowOptimizedAlgorithm());

            return algorithms;
        }

        private PositionCandidate SelectBestCandidate(List<PositionCandidate> candidates, PositioningContext context)
        {
            foreach (var candidate in candidates)
            {
                var score = 0.0;

                score += candidate.UsagePatternScore * 0.4;

                score += candidate.CollisionScore * 0.25;

                score += candidate.AestheticScore * 0.2;

                score += candidate.PerformanceScore * 0.1;

                score += candidate.UserPreferenceScore * 0.05;

                candidate.Score = score;
            }

            return candidates.OrderByDescending(c => c.Score).First();
        }

        public void TrainFromUserFeedback(IntPtr windowHandle, Rectangle actualBounds, UserFeedback feedback)
        {
            var windowInfo = windowManager.GetWindowInfo(windowHandle);
            if (windowInfo != null)
            {
                usageAnalyzer.RecordUserFeedback(windowInfo.ProcessName, actualBounds, feedback);
            }
        }

        public SmartMarginSettings CalculateAdaptiveMargins(MonitorInfo monitor, WindowInfo window, MarginSettings baseSettings)
        {
            return marginCalculator.CalculateAdaptiveMargins(monitor, window, baseSettings);
        }

        public List<WindowCluster> AnalyzeWindowClusters(List<WindowInfo> windows)
        {
            return collisionDetector.AnalyzeWindowClusters(windows);
        }

        public UsageStatistics GetUsageStatistics()
        {
            return usageAnalyzer.GetUsageStatistics();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                usageAnalyzer?.Dispose();
                disposed = true;
            }
        }
    }

    public class SmartPositionOptions
    {
        public bool UseUsagePatterns { get; set; } = true;
        public bool UseGoldenRatio { get; set; } = true;
        public bool UseContentAware { get; set; } = true;
        public bool UseCollisionAvoidance { get; set; } = true;
        public bool UseAdaptiveMargins { get; set; } = true;
        public bool PreferPrimaryMonitor { get; set; } = false;
        public WindowPriority Priority { get; set; } = WindowPriority.Normal;
        public List<string> AvoidApplications { get; set; } = new List<string>();
        public Rectangle? PreferredZone { get; set; }
    }

    public class SmartPositionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Rectangle RecommendedBounds { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public List<AlternativePosition> AlternativePositions { get; set; } = new List<AlternativePosition>();
    }

    public class AlternativePosition
    {
        public Rectangle Bounds { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PositioningContext
    {
        public WindowInfo WindowInfo { get; set; } = new WindowInfo();
        public MonitorInfo Monitor { get; set; } = new MonitorInfo();
        public SmartPositionOptions Options { get; set; } = new SmartPositionOptions();
        public List<WindowInfo> ExistingWindows { get; set; } = new List<WindowInfo>();
        public WindowUsageHistory UsageHistory { get; set; } = new WindowUsageHistory();
    }

    public class PositionCandidate
    {
        public Rectangle Bounds { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public double UsagePatternScore { get; set; }
        public double CollisionScore { get; set; }
        public double AestheticScore { get; set; }
        public double PerformanceScore { get; set; }
        public double UserPreferenceScore { get; set; }
    }

    public enum WindowPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum UserFeedback
    {
        Perfect,
        Good,
        Acceptable,
        Poor,
        Terrible
    }

    public class SmartMarginSettings : MarginSettings
    {
        public double AdaptiveMultiplier { get; set; } = 1.0;
        public string CalculationMethod { get; set; } = string.Empty;
        public Dictionary<string, object> CalculationParameters { get; set; } = new Dictionary<string, object>();
    }
}
