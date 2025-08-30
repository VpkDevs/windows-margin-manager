using System;
using System.Drawing;

namespace WindowsMarginManager
{
    public class GoldenRatioCalculator
    {
        private const double GOLDEN_RATIO = 1.618033988749;
        private const double INVERSE_GOLDEN_RATIO = 0.618033988749;

        public Rectangle CalculateGoldenRatioBounds(Rectangle workingArea, WindowInfo window)
        {
            var strategy = DetermineGoldenRatioStrategy(window);
            
            return strategy switch
            {
                GoldenRatioStrategy.CenterGolden => CalculateCenterGoldenBounds(workingArea),
                GoldenRatioStrategy.LeftGolden => CalculateLeftGoldenBounds(workingArea),
                GoldenRatioStrategy.RightGolden => CalculateRightGoldenBounds(workingArea),
                GoldenRatioStrategy.TopGolden => CalculateTopGoldenBounds(workingArea),
                GoldenRatioStrategy.BottomGolden => CalculateBottomGoldenBounds(workingArea),
                GoldenRatioStrategy.GoldenGrid => CalculateGoldenGridBounds(workingArea),
                _ => CalculateCenterGoldenBounds(workingArea)
            };
        }

        public Point CalculateGoldenRatioPoint(Rectangle area, GoldenRatioPosition position)
        {
            return position switch
            {
                GoldenRatioPosition.TopLeft => new Point(
                    area.X + (int)(area.Width * INVERSE_GOLDEN_RATIO),
                    area.Y + (int)(area.Height * INVERSE_GOLDEN_RATIO)
                ),
                GoldenRatioPosition.TopRight => new Point(
                    area.X + (int)(area.Width * (1 - INVERSE_GOLDEN_RATIO)),
                    area.Y + (int)(area.Height * INVERSE_GOLDEN_RATIO)
                ),
                GoldenRatioPosition.BottomLeft => new Point(
                    area.X + (int)(area.Width * INVERSE_GOLDEN_RATIO),
                    area.Y + (int)(area.Height * (1 - INVERSE_GOLDEN_RATIO))
                ),
                GoldenRatioPosition.BottomRight => new Point(
                    area.X + (int)(area.Width * (1 - INVERSE_GOLDEN_RATIO)),
                    area.Y + (int)(area.Height * (1 - INVERSE_GOLDEN_RATIO))
                ),
                GoldenRatioPosition.Center => new Point(
                    area.X + area.Width / 2,
                    area.Y + area.Height / 2
                ),
                _ => new Point(area.X + area.Width / 2, area.Y + area.Height / 2)
            };
        }

        public Size CalculateGoldenRatioSize(Rectangle area, double scaleFactor = 0.8)
        {
            var maxWidth = (int)(area.Width * scaleFactor);
            var maxHeight = (int)(area.Height * scaleFactor);
            
            var goldenWidth = (int)(maxHeight * GOLDEN_RATIO);
            var goldenHeight = (int)(maxWidth / GOLDEN_RATIO);
            
            if (goldenWidth <= maxWidth)
            {
                return new Size(goldenWidth, maxHeight);
            }
            else if (goldenHeight <= maxHeight)
            {
                return new Size(maxWidth, goldenHeight);
            }
            else
            {
                var ratio = Math.Min((double)maxWidth / GOLDEN_RATIO, maxHeight);
                return new Size((int)(ratio * GOLDEN_RATIO), (int)ratio);
            }
        }

        public Rectangle[] DivideByGoldenRatio(Rectangle area, GoldenRatioDivision division)
        {
            return division switch
            {
                GoldenRatioDivision.Horizontal => DivideHorizontallyByGoldenRatio(area),
                GoldenRatioDivision.Vertical => DivideVerticallyByGoldenRatio(area),
                GoldenRatioDivision.Grid => DivideByGoldenGrid(area),
                _ => new[] { area }
            };
        }

        private GoldenRatioStrategy DetermineGoldenRatioStrategy(WindowInfo window)
        {
            var processName = window.ProcessName.ToLower();
            
            if (IsDesignApp(processName))
                return GoldenRatioStrategy.CenterGolden;
            else if (IsProductivityApp(processName))
                return GoldenRatioStrategy.LeftGolden;
            else if (IsMediaApp(processName))
                return GoldenRatioStrategy.CenterGolden;
            else if (IsBrowserApp(processName))
                return GoldenRatioStrategy.GoldenGrid;
            else if (IsCodeEditorApp(processName))
                return GoldenRatioStrategy.LeftGolden;
            else
                return GoldenRatioStrategy.CenterGolden;
        }

        private Rectangle CalculateCenterGoldenBounds(Rectangle workingArea)
        {
            var size = CalculateGoldenRatioSize(workingArea, 0.8);
            var x = workingArea.X + (workingArea.Width - size.Width) / 2;
            var y = workingArea.Y + (workingArea.Height - size.Height) / 2;
            
            return new Rectangle(x, y, size.Width, size.Height);
        }

        private Rectangle CalculateLeftGoldenBounds(Rectangle workingArea)
        {
            var width = (int)(workingArea.Width * INVERSE_GOLDEN_RATIO);
            var height = (int)(workingArea.Height * 0.9);
            var x = workingArea.X + 20;
            var y = workingArea.Y + (workingArea.Height - height) / 2;
            
            return new Rectangle(x, y, width, height);
        }

        private Rectangle CalculateRightGoldenBounds(Rectangle workingArea)
        {
            var width = (int)(workingArea.Width * INVERSE_GOLDEN_RATIO);
            var height = (int)(workingArea.Height * 0.9);
            var x = workingArea.Right - width - 20;
            var y = workingArea.Y + (workingArea.Height - height) / 2;
            
            return new Rectangle(x, y, width, height);
        }

        private Rectangle CalculateTopGoldenBounds(Rectangle workingArea)
        {
            var width = (int)(workingArea.Width * 0.9);
            var height = (int)(workingArea.Height * INVERSE_GOLDEN_RATIO);
            var x = workingArea.X + (workingArea.Width - width) / 2;
            var y = workingArea.Y + 20;
            
            return new Rectangle(x, y, width, height);
        }

        private Rectangle CalculateBottomGoldenBounds(Rectangle workingArea)
        {
            var width = (int)(workingArea.Width * 0.9);
            var height = (int)(workingArea.Height * INVERSE_GOLDEN_RATIO);
            var x = workingArea.X + (workingArea.Width - width) / 2;
            var y = workingArea.Bottom - height - 20;
            
            return new Rectangle(x, y, width, height);
        }

        private Rectangle CalculateGoldenGridBounds(Rectangle workingArea)
        {
            var goldenPoint = CalculateGoldenRatioPoint(workingArea, GoldenRatioPosition.TopLeft);
            var size = CalculateGoldenRatioSize(workingArea, 0.6);
            
            var x = goldenPoint.X - size.Width / 2;
            var y = goldenPoint.Y - size.Height / 2;
            
            x = Math.Max(workingArea.X, Math.Min(x, workingArea.Right - size.Width));
            y = Math.Max(workingArea.Y, Math.Min(y, workingArea.Bottom - size.Height));
            
            return new Rectangle(x, y, size.Width, size.Height);
        }

        private Rectangle[] DivideHorizontallyByGoldenRatio(Rectangle area)
        {
            var divisionPoint = (int)(area.Height * INVERSE_GOLDEN_RATIO);
            
            var topRect = new Rectangle(area.X, area.Y, area.Width, divisionPoint);
            var bottomRect = new Rectangle(area.X, area.Y + divisionPoint, area.Width, area.Height - divisionPoint);
            
            return new[] { topRect, bottomRect };
        }

        private Rectangle[] DivideVerticallyByGoldenRatio(Rectangle area)
        {
            var divisionPoint = (int)(area.Width * INVERSE_GOLDEN_RATIO);
            
            var leftRect = new Rectangle(area.X, area.Y, divisionPoint, area.Height);
            var rightRect = new Rectangle(area.X + divisionPoint, area.Y, area.Width - divisionPoint, area.Height);
            
            return new[] { leftRect, rightRect };
        }

        private Rectangle[] DivideByGoldenGrid(Rectangle area)
        {
            var verticalDivision = DivideVerticallyByGoldenRatio(area);
            var leftHorizontal = DivideHorizontallyByGoldenRatio(verticalDivision[0]);
            var rightHorizontal = DivideHorizontallyByGoldenRatio(verticalDivision[1]);
            
            return new[] { leftHorizontal[0], leftHorizontal[1], rightHorizontal[0], rightHorizontal[1] };
        }

        private bool IsDesignApp(string processName)
        {
            var designApps = new[] { "photoshop", "illustrator", "indesign", "figma", "sketch", "canva", "gimp", "inkscape" };
            return designApps.Any(app => processName.Contains(app));
        }

        private bool IsProductivityApp(string processName)
        {
            var productivityApps = new[] { "word", "excel", "powerpoint", "outlook", "onenote", "teams", "slack", "notion" };
            return productivityApps.Any(app => processName.Contains(app));
        }

        private bool IsMediaApp(string processName)
        {
            var mediaApps = new[] { "vlc", "wmplayer", "spotify", "itunes", "netflix", "youtube", "plex", "kodi" };
            return mediaApps.Any(app => processName.Contains(app));
        }

        private bool IsBrowserApp(string processName)
        {
            var browserApps = new[] { "chrome", "firefox", "edge", "safari", "opera", "brave" };
            return browserApps.Any(app => processName.Contains(app));
        }

        private bool IsCodeEditorApp(string processName)
        {
            var codeEditors = new[] { "code", "devenv", "sublime", "atom", "notepad++", "vim", "emacs", "intellij", "pycharm" };
            return codeEditors.Any(app => processName.Contains(app));
        }
    }

    public enum GoldenRatioStrategy
    {
        CenterGolden,
        LeftGolden,
        RightGolden,
        TopGolden,
        BottomGolden,
        GoldenGrid
    }

    public enum GoldenRatioPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    public enum GoldenRatioDivision
    {
        Horizontal,
        Vertical,
        Grid
    }
}
