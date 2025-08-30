using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindowsMarginManager
{
    public class CollisionDetector
    {
        public Rectangle FindOptimalPosition(Size desiredSize, Rectangle workingArea, List<WindowInfo> existingWindows)
        {
            var candidates = GeneratePositionCandidates(desiredSize, workingArea);
            
            foreach (var candidate in candidates)
            {
                if (!HasCollisions(candidate, existingWindows))
                {
                    return candidate;
                }
            }

            return FindMinimalOverlapPosition(desiredSize, workingArea, existingWindows);
        }

        public List<WindowCluster> AnalyzeWindowClusters(List<WindowInfo> windows)
        {
            var clusters = new List<WindowCluster>();
            var processed = new HashSet<WindowInfo>();

            foreach (var window in windows)
            {
                if (processed.Contains(window))
                    continue;

                var cluster = new WindowCluster();
                var toProcess = new Queue<WindowInfo>();
                toProcess.Enqueue(window);

                while (toProcess.Count > 0)
                {
                    var current = toProcess.Dequeue();
                    if (processed.Contains(current))
                        continue;

                    processed.Add(current);
                    cluster.Windows.Add(current);

                    var nearby = windows.Where(w => 
                        !processed.Contains(w) && 
                        IsNearby(current.Bounds, w.Bounds, 100)).ToList();

                    foreach (var nearbyWindow in nearby)
                    {
                        toProcess.Enqueue(nearbyWindow);
                    }
                }

                if (cluster.Windows.Any())
                {
                    cluster.BoundingBox = CalculateBoundingBox(cluster.Windows);
                    cluster.Density = CalculateClusterDensity(cluster);
                    clusters.Add(cluster);
                }
            }

            return clusters.OrderByDescending(c => c.Density).ToList();
        }

        public double CalculateOverlapPercentage(Rectangle rect1, Rectangle rect2)
        {
            var intersection = Rectangle.Intersect(rect1, rect2);
            if (intersection.IsEmpty)
                return 0.0;

            var intersectionArea = intersection.Width * intersection.Height;
            var rect1Area = rect1.Width * rect1.Height;
            var rect2Area = rect2.Width * rect2.Height;
            var unionArea = rect1Area + rect2Area - intersectionArea;

            return (double)intersectionArea / unionArea;
        }

        public List<Rectangle> FindAvailableSpaces(Rectangle workingArea, List<WindowInfo> existingWindows, Size minimumSize)
        {
            var availableSpaces = new List<Rectangle>();
            var gridSize = 50; // 50-pixel grid for analysis

            for (int x = workingArea.X; x <= workingArea.Right - minimumSize.Width; x += gridSize)
            {
                for (int y = workingArea.Y; y <= workingArea.Bottom - minimumSize.Height; y += gridSize)
                {
                    var testRect = new Rectangle(x, y, minimumSize.Width, minimumSize.Height);
                    
                    if (!HasCollisions(testRect, existingWindows))
                    {
                        var expandedSpace = ExpandAvailableSpace(testRect, workingArea, existingWindows);
                        
                        if (!availableSpaces.Any(space => space.IntersectsWith(expandedSpace)))
                        {
                            availableSpaces.Add(expandedSpace);
                        }
                    }
                }
            }

            return availableSpaces.OrderByDescending(space => space.Width * space.Height).ToList();
        }

        private List<Rectangle> GeneratePositionCandidates(Size desiredSize, Rectangle workingArea)
        {
            var candidates = new List<Rectangle>();
            var padding = 20;

            var centerX = workingArea.X + (workingArea.Width - desiredSize.Width) / 2;
            var centerY = workingArea.Y + (workingArea.Height - desiredSize.Height) / 2;
            candidates.Add(new Rectangle(centerX, centerY, desiredSize.Width, desiredSize.Height));

            candidates.Add(new Rectangle(workingArea.X + padding, workingArea.Y + padding, desiredSize.Width, desiredSize.Height));
            candidates.Add(new Rectangle(workingArea.Right - desiredSize.Width - padding, workingArea.Y + padding, desiredSize.Width, desiredSize.Height));
            candidates.Add(new Rectangle(workingArea.X + padding, workingArea.Bottom - desiredSize.Height - padding, desiredSize.Width, desiredSize.Height));
            candidates.Add(new Rectangle(workingArea.Right - desiredSize.Width - padding, workingArea.Bottom - desiredSize.Height - padding, desiredSize.Width, desiredSize.Height));

            candidates.Add(new Rectangle(workingArea.X + padding, centerY, desiredSize.Width, desiredSize.Height)); // Left edge
            candidates.Add(new Rectangle(workingArea.Right - desiredSize.Width - padding, centerY, desiredSize.Width, desiredSize.Height)); // Right edge
            candidates.Add(new Rectangle(centerX, workingArea.Y + padding, desiredSize.Width, desiredSize.Height)); // Top edge
            candidates.Add(new Rectangle(centerX, workingArea.Bottom - desiredSize.Height - padding, desiredSize.Width, desiredSize.Height)); // Bottom edge

            var gridCols = 4;
            var gridRows = 3;
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    var x = workingArea.X + (col * workingArea.Width / gridCols) + padding;
                    var y = workingArea.Y + (row * workingArea.Height / gridRows) + padding;
                    
                    if (x + desiredSize.Width <= workingArea.Right - padding &&
                        y + desiredSize.Height <= workingArea.Bottom - padding)
                    {
                        candidates.Add(new Rectangle(x, y, desiredSize.Width, desiredSize.Height));
                    }
                }
            }

            return candidates.Where(c => workingArea.Contains(c)).ToList();
        }

        private bool HasCollisions(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            return existingWindows.Any(window => window.Bounds.IntersectsWith(bounds));
        }

        private Rectangle FindMinimalOverlapPosition(Size desiredSize, Rectangle workingArea, List<WindowInfo> existingWindows)
        {
            var candidates = GeneratePositionCandidates(desiredSize, workingArea);
            
            var bestCandidate = candidates
                .Select(candidate => new
                {
                    Bounds = candidate,
                    OverlapScore = CalculateTotalOverlap(candidate, existingWindows)
                })
                .OrderBy(c => c.OverlapScore)
                .First();

            return bestCandidate.Bounds;
        }

        private double CalculateTotalOverlap(Rectangle bounds, List<WindowInfo> existingWindows)
        {
            return existingWindows.Sum(window => CalculateOverlapPercentage(bounds, window.Bounds));
        }

        private bool IsNearby(Rectangle rect1, Rectangle rect2, int threshold)
        {
            var center1 = new Point(rect1.X + rect1.Width / 2, rect1.Y + rect1.Height / 2);
            var center2 = new Point(rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2);
            
            var distance = Math.Sqrt(Math.Pow(center1.X - center2.X, 2) + Math.Pow(center1.Y - center2.Y, 2));
            return distance <= threshold;
        }

        private Rectangle CalculateBoundingBox(List<WindowInfo> windows)
        {
            if (!windows.Any())
                return Rectangle.Empty;

            var minX = windows.Min(w => w.Bounds.X);
            var minY = windows.Min(w => w.Bounds.Y);
            var maxX = windows.Max(w => w.Bounds.Right);
            var maxY = windows.Max(w => w.Bounds.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private double CalculateClusterDensity(WindowCluster cluster)
        {
            if (!cluster.Windows.Any())
                return 0.0;

            var totalWindowArea = cluster.Windows.Sum(w => w.Bounds.Width * w.Bounds.Height);
            var boundingBoxArea = cluster.BoundingBox.Width * cluster.BoundingBox.Height;

            return boundingBoxArea > 0 ? (double)totalWindowArea / boundingBoxArea : 0.0;
        }

        private Rectangle ExpandAvailableSpace(Rectangle initialSpace, Rectangle workingArea, List<WindowInfo> existingWindows)
        {
            var expanded = initialSpace;
            var maxExpansions = 20; // Prevent infinite loops
            var expansions = 0;

            while (expansions < maxExpansions)
            {
                var expandedRight = new Rectangle(expanded.X, expanded.Y, expanded.Width + 50, expanded.Height);
                var expandedDown = new Rectangle(expanded.X, expanded.Y, expanded.Width, expanded.Height + 50);
                var expandedBoth = new Rectangle(expanded.X, expanded.Y, expanded.Width + 50, expanded.Height + 50);

                var canExpandRight = workingArea.Contains(expandedRight) && !HasCollisions(expandedRight, existingWindows);
                var canExpandDown = workingArea.Contains(expandedDown) && !HasCollisions(expandedDown, existingWindows);
                var canExpandBoth = workingArea.Contains(expandedBoth) && !HasCollisions(expandedBoth, existingWindows);

                if (canExpandBoth)
                {
                    expanded = expandedBoth;
                }
                else if (canExpandRight)
                {
                    expanded = expandedRight;
                }
                else if (canExpandDown)
                {
                    expanded = expandedDown;
                }
                else
                {
                    break; // No more expansion possible
                }

                expansions++;
            }

            return expanded;
        }
    }

    public class WindowCluster
    {
        public List<WindowInfo> Windows { get; set; } = new List<WindowInfo>();
        public Rectangle BoundingBox { get; set; }
        public double Density { get; set; }
    }
}
