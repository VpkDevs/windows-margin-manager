using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindowsMarginManager
{
    public interface IPositioningAlgorithm
    {
        PositionCandidate? CalculatePosition(PositioningContext context);
    }

    public class UsagePatternAlgorithm : IPositioningAlgorithm
    {
        private readonly UsageAnalyzer usageAnalyzer;

        public UsagePatternAlgorithm(UsageAnalyzer usageAnalyzer)
        {
            this.usageAnalyzer = usageAnalyzer;
        }

        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var predictedPosition = usageAnalyzer.PredictOptimalPosition(
                context.WindowInfo.ProcessName, context.Monitor);

            if (predictedPosition == null)
                return null;

            var bounds = predictedPosition.Value;
            
            bounds = EnsureBoundsWithinMonitor(bounds, context.Monitor);

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Usage Pattern",
                UsagePatternScore = 0.9,
                CollisionScore = CalculateCollisionScore(bounds, context.ExistingWindows),
                AestheticScore = 0.7,
                PerformanceScore = 0.8,
                UserPreferenceScore = 0.9,
                Reasoning = "Based on historical usage patterns for this application"
            };
        }

        private Rectangle EnsureBoundsWithinMonitor(Rectangle bounds, MonitorInfo monitor)
        {
            var workingArea = monitor.WorkingArea;
            
            if (bounds.X < workingArea.X)
                bounds.X = workingArea.X;
            if (bounds.Y < workingArea.Y)
                bounds.Y = workingArea.Y;
            
            if (bounds.Width > workingArea.Width)
                bounds.Width = workingArea.Width;
            if (bounds.Height > workingArea.Height)
                bounds.Height = workingArea.Height;
            
            if (bounds.Right > workingArea.Right)
                bounds.X = workingArea.Right - bounds.Width;
            if (bounds.Bottom > workingArea.Bottom)
                bounds.Y = workingArea.Bottom - bounds.Height;

            return bounds;
        }

        private double CalculateCollisionScore(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            var collisions = existingWindows.Count(w => w.Bounds.IntersectsWith(bounds));
            return Math.Max(0, 1.0 - (collisions * 0.3));
        }
    }

    public class GoldenRatioAlgorithm : IPositioningAlgorithm
    {
        private readonly GoldenRatioCalculator calculator;
        private const double GOLDEN_RATIO = 1.618033988749;

        public GoldenRatioAlgorithm(GoldenRatioCalculator calculator)
        {
            this.calculator = calculator;
        }

        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var workingArea = context.Monitor.WorkingArea;
            var bounds = calculator.CalculateGoldenRatioBounds(workingArea, context.WindowInfo);

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Golden Ratio",
                UsagePatternScore = 0.5,
                CollisionScore = CalculateCollisionScore(bounds, context.ExistingWindows),
                AestheticScore = 0.95,
                PerformanceScore = 0.9,
                UserPreferenceScore = 0.6,
                Reasoning = "Positioned using golden ratio for optimal aesthetic proportions"
            };
        }

        private double CalculateCollisionScore(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            var collisions = existingWindows.Count(w => w.Bounds.IntersectsWith(bounds));
            return Math.Max(0, 1.0 - (collisions * 0.3));
        }
    }

    public class ContentAwareAlgorithm : IPositioningAlgorithm
    {
        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var workingArea = context.Monitor.WorkingArea;
            var bounds = CalculateContentAwareBounds(context.WindowInfo, workingArea);

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Content Aware",
                UsagePatternScore = 0.6,
                CollisionScore = CalculateCollisionScore(bounds, context.ExistingWindows),
                AestheticScore = 0.8,
                PerformanceScore = 0.85,
                UserPreferenceScore = 0.7,
                Reasoning = "Optimized based on application type and content requirements"
            };
        }

        private Rectangle CalculateContentAwareBounds(WindowInfo window, Rectangle workingArea)
        {
            var processName = window.ProcessName.ToLower();
            
            if (IsCodeEditor(processName))
            {
                var width = (int)(workingArea.Width * 0.7);
                var height = (int)(workingArea.Height * 0.8);
                var x = workingArea.X + (workingArea.Width - width) / 2;
                var y = workingArea.Y + (workingArea.Height - height) / 2;
                return new Rectangle(x, y, width, height);
            }
            else if (IsBrowser(processName))
            {
                var width = (int)(workingArea.Width * 0.8);
                var height = (int)(workingArea.Height * 0.9);
                var x = workingArea.X + (workingArea.Width - width) / 2;
                var y = workingArea.Y;
                return new Rectangle(x, y, width, height);
            }
            else if (IsMediaPlayer(processName))
            {
                var width = (int)(workingArea.Width * 0.6);
                var height = (int)(width / 1.777); // 16:9 ratio
                var x = workingArea.X + (workingArea.Width - width) / 2;
                var y = workingArea.Y + (workingArea.Height - height) / 2;
                return new Rectangle(x, y, width, height);
            }
            else if (IsUtility(processName))
            {
                var width = (int)(workingArea.Width * 0.4);
                var height = (int)(workingArea.Height * 0.5);
                var x = workingArea.Right - width - 50;
                var y = workingArea.Y + 50;
                return new Rectangle(x, y, width, height);
            }
            
            var defaultWidth = (int)(workingArea.Width * 0.6);
            var defaultHeight = (int)(workingArea.Height * 0.7);
            var defaultX = workingArea.X + (workingArea.Width - defaultWidth) / 2;
            var defaultY = workingArea.Y + (workingArea.Height - defaultHeight) / 2;
            return new Rectangle(defaultX, defaultY, defaultWidth, defaultHeight);
        }

        private bool IsCodeEditor(string processName)
        {
            var codeEditors = new[] { "code", "devenv", "notepad++", "sublime", "atom", "vim", "emacs" };
            return codeEditors.Any(editor => processName.Contains(editor));
        }

        private bool IsBrowser(string processName)
        {
            var browsers = new[] { "chrome", "firefox", "edge", "safari", "opera", "brave" };
            return browsers.Any(browser => processName.Contains(browser));
        }

        private bool IsMediaPlayer(string processName)
        {
            var mediaPlayers = new[] { "vlc", "wmplayer", "spotify", "itunes", "foobar", "winamp" };
            return mediaPlayers.Any(player => processName.Contains(player));
        }

        private bool IsUtility(string processName)
        {
            var utilities = new[] { "calculator", "notepad", "cmd", "powershell", "regedit", "taskmgr" };
            return utilities.Any(utility => processName.Contains(utility));
        }

        private double CalculateCollisionScore(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            var collisions = existingWindows.Count(w => w.Bounds.IntersectsWith(bounds));
            return Math.Max(0, 1.0 - (collisions * 0.3));
        }
    }

    public class CollisionAvoidanceAlgorithm : IPositioningAlgorithm
    {
        private readonly CollisionDetector collisionDetector;

        public CollisionAvoidanceAlgorithm(CollisionDetector collisionDetector)
        {
            this.collisionDetector = collisionDetector;
        }

        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var workingArea = context.Monitor.WorkingArea;
            var desiredSize = new Size(
                (int)(workingArea.Width * 0.6),
                (int)(workingArea.Height * 0.7)
            );

            var optimalPosition = collisionDetector.FindOptimalPosition(
                desiredSize, workingArea, context.ExistingWindows);

            if (optimalPosition == Rectangle.Empty)
                return null;

            return new PositionCandidate
            {
                Bounds = optimalPosition,
                Algorithm = "Collision Avoidance",
                UsagePatternScore = 0.5,
                CollisionScore = 1.0, // Perfect collision avoidance
                AestheticScore = 0.6,
                PerformanceScore = 0.9,
                UserPreferenceScore = 0.5,
                Reasoning = "Positioned to avoid overlapping with existing windows"
            };
        }
    }

    public class AdaptiveMarginAlgorithm : IPositioningAlgorithm
    {
        private readonly AdaptiveMarginCalculator marginCalculator;

        public AdaptiveMarginAlgorithm(AdaptiveMarginCalculator marginCalculator)
        {
            this.marginCalculator = marginCalculator;
        }

        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var baseSettings = new MarginSettings
            {
                LeftMargin = 50,
                TopMargin = 50,
                RightMargin = 50,
                BottomMargin = 50
            };

            var adaptiveSettings = marginCalculator.CalculateAdaptiveMargins(
                context.Monitor, context.WindowInfo, baseSettings);

            var workingArea = context.Monitor.WorkingArea;
            var bounds = new Rectangle(
                workingArea.X + adaptiveSettings.LeftMargin,
                workingArea.Y + adaptiveSettings.TopMargin,
                workingArea.Width - adaptiveSettings.LeftMargin - adaptiveSettings.RightMargin,
                workingArea.Height - adaptiveSettings.TopMargin - adaptiveSettings.BottomMargin
            );

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Adaptive Margin",
                UsagePatternScore = 0.7,
                CollisionScore = CalculateCollisionScore(bounds, context.ExistingWindows),
                AestheticScore = 0.8,
                PerformanceScore = 0.85,
                UserPreferenceScore = 0.8,
                Reasoning = $"Adaptive margins calculated using {adaptiveSettings.CalculationMethod}"
            };
        }

        private double CalculateCollisionScore(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            var collisions = existingWindows.Count(w => w.Bounds.IntersectsWith(bounds));
            return Math.Max(0, 1.0 - (collisions * 0.3));
        }
    }

    public class ScreenZoneAlgorithm : IPositioningAlgorithm
    {
        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var workingArea = context.Monitor.WorkingArea;
            var zones = DivideIntoZones(workingArea, 3, 3); // 3x3 grid
            
            var bestZone = zones
                .Select(zone => new
                {
                    Zone = zone,
                    Occupancy = CalculateZoneOccupancy(zone, context.ExistingWindows)
                })
                .OrderBy(z => z.Occupancy)
                .First().Zone;

            var padding = 20;
            var bounds = new Rectangle(
                bestZone.X + padding,
                bestZone.Y + padding,
                bestZone.Width - (padding * 2),
                bestZone.Height - (padding * 2)
            );

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Screen Zone",
                UsagePatternScore = 0.6,
                CollisionScore = 0.9,
                AestheticScore = 0.75,
                PerformanceScore = 0.9,
                UserPreferenceScore = 0.6,
                Reasoning = "Positioned in the least occupied screen zone"
            };
        }

        private List<Rectangle> DivideIntoZones(Rectangle area, int rows, int cols)
        {
            var zones = new List<Rectangle>();
            var zoneWidth = area.Width / cols;
            var zoneHeight = area.Height / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    zones.Add(new Rectangle(
                        area.X + (col * zoneWidth),
                        area.Y + (row * zoneHeight),
                        zoneWidth,
                        zoneHeight
                    ));
                }
            }

            return zones;
        }

        private double CalculateZoneOccupancy(Rectangle zone, List<WindowInfo> windows)
        {
            var totalOverlap = 0.0;
            var zoneArea = zone.Width * zone.Height;

            foreach (var window in windows)
            {
                var intersection = Rectangle.Intersect(zone, window.Bounds);
                if (!intersection.IsEmpty)
                {
                    totalOverlap += intersection.Width * intersection.Height;
                }
            }

            return totalOverlap / zoneArea;
        }
    }

    public class WorkflowOptimizedAlgorithm : IPositioningAlgorithm
    {
        public PositionCandidate? CalculatePosition(PositioningContext context)
        {
            var workingArea = context.Monitor.WorkingArea;
            var bounds = CalculateWorkflowOptimizedBounds(context.WindowInfo, workingArea, context.ExistingWindows);

            return new PositionCandidate
            {
                Bounds = bounds,
                Algorithm = "Workflow Optimized",
                UsagePatternScore = 0.8,
                CollisionScore = CalculateCollisionScore(bounds, context.ExistingWindows),
                AestheticScore = 0.7,
                PerformanceScore = 0.85,
                UserPreferenceScore = 0.9,
                Reasoning = "Optimized for common workflow patterns and productivity"
            };
        }

        private Rectangle CalculateWorkflowOptimizedBounds(WindowInfo window, Rectangle workingArea, List<WindowInfo> existingWindows)
        {
            var processName = window.ProcessName.ToLower();
            
            if (HasComplementaryApp(processName, existingWindows))
            {
                return PositionForComplementaryWorkflow(processName, workingArea, existingWindows);
            }

            var width = (int)(workingArea.Width * 0.65);
            var height = (int)(workingArea.Height * 0.8);
            var x = workingArea.X + (workingArea.Width - width) / 2;
            var y = workingArea.Y + (workingArea.Height - height) / 2;

            return new Rectangle(x, y, width, height);
        }

        private bool HasComplementaryApp(string processName, List<WindowInfo> existingWindows)
        {
            var complementaryPairs = new Dictionary<string, string[]>
            {
                ["code"] = new[] { "chrome", "firefox", "cmd", "powershell" },
                ["chrome"] = new[] { "code", "notepad", "sublime" },
                ["photoshop"] = new[] { "bridge", "lightroom" },
                ["excel"] = new[] { "word", "powerpoint" }
            };

            if (!complementaryPairs.ContainsKey(processName))
                return false;

            var complementary = complementaryPairs[processName];
            return existingWindows.Any(w => complementary.Any(c => w.ProcessName.ToLower().Contains(c)));
        }

        private Rectangle PositionForComplementaryWorkflow(string processName, Rectangle workingArea, List<WindowInfo> existingWindows)
        {
            var width = workingArea.Width / 2 - 10;
            var height = (int)(workingArea.Height * 0.9);
            
            var leftSide = new Rectangle(workingArea.X, workingArea.Y, width, height);
            var rightSide = new Rectangle(workingArea.X + width + 20, workingArea.Y, width, height);
            
            var leftOccupied = existingWindows.Any(w => w.Bounds.IntersectsWith(leftSide));
            var rightOccupied = existingWindows.Any(w => w.Bounds.IntersectsWith(rightSide));
            
            if (!leftOccupied)
                return leftSide;
            else if (!rightOccupied)
                return rightSide;
            else
                return leftSide; // Default to left if both occupied
        }

        private double CalculateCollisionScore(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            var collisions = existingWindows.Count(w => w.Bounds.IntersectsWith(bounds));
            return Math.Max(0, 1.0 - (collisions * 0.3));
        }
    }
}
