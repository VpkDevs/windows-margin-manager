using System;
using System.IO;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class MarginSettings
    {
        public int LeftMargin { get; set; } = 50;
        public int TopMargin { get; set; } = 50;
        public int RightMargin { get; set; } = 50;
        public int BottomMargin { get; set; } = 50;
        public bool UsePercentage { get; set; } = false;
        public bool EnableAnimations { get; set; } = true;
        public int AnimationDuration { get; set; } = 300;
        public EasingType AnimationEasing { get; set; } = EasingType.EaseOutCubic;
        public MarginType MarginType { get; set; } = MarginType.Fixed;
        public bool AdaptiveMargins { get; set; } = false;
        public bool GoldenRatioMode { get; set; } = false;
        public double CustomRatio { get; set; } = 1.618;
        public bool RespectMinimumSizes { get; set; } = true;
        public bool PreserveAspectRatio { get; set; } = false;
        public bool EnableCollisionDetection { get; set; } = true;
        public bool EnableSnapZones { get; set; } = true;
        public int SnapThreshold { get; set; } = 20;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsMarginManager",
            "settings.json"
        );

        public MarginSettings()
        {
            LoadSettings();
        }

        public MarginSettings(MarginSettings other)
        {
            LeftMargin = other.LeftMargin;
            TopMargin = other.TopMargin;
            RightMargin = other.RightMargin;
            BottomMargin = other.BottomMargin;
            UsePercentage = other.UsePercentage;
            EnableAnimations = other.EnableAnimations;
            AnimationDuration = other.AnimationDuration;
            AnimationEasing = other.AnimationEasing;
            MarginType = other.MarginType;
            AdaptiveMargins = other.AdaptiveMargins;
            GoldenRatioMode = other.GoldenRatioMode;
            CustomRatio = other.CustomRatio;
            RespectMinimumSizes = other.RespectMinimumSizes;
            PreserveAspectRatio = other.PreserveAspectRatio;
            EnableCollisionDetection = other.EnableCollisionDetection;
            EnableSnapZones = other.EnableSnapZones;
            SnapThreshold = other.SnapThreshold;
        }

        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to save settings: {ex.Message}", 
                    "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<MarginSettings>(json);
                    
                    if (settings != null)
                    {
                        CopyFrom(settings);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to load settings, using defaults: {ex.Message}", 
                    "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        private void CopyFrom(MarginSettings other)
        {
            LeftMargin = other.LeftMargin;
            TopMargin = other.TopMargin;
            RightMargin = other.RightMargin;
            BottomMargin = other.BottomMargin;
            UsePercentage = other.UsePercentage;
            EnableAnimations = other.EnableAnimations;
            AnimationDuration = other.AnimationDuration;
            AnimationEasing = other.AnimationEasing;
            MarginType = other.MarginType;
            AdaptiveMargins = other.AdaptiveMargins;
            GoldenRatioMode = other.GoldenRatioMode;
            CustomRatio = other.CustomRatio;
            RespectMinimumSizes = other.RespectMinimumSizes;
            PreserveAspectRatio = other.PreserveAspectRatio;
            EnableCollisionDetection = other.EnableCollisionDetection;
            EnableSnapZones = other.EnableSnapZones;
            SnapThreshold = other.SnapThreshold;
        }

        public (int left, int top, int right, int bottom) CalculateMargins(int screenWidth, int screenHeight, double dpiScale = 1.0)
        {
            int left, top, right, bottom;

            switch (MarginType)
            {
                case MarginType.Percentage:
                    left = (int)(screenWidth * LeftMargin / 100.0 * dpiScale);
                    top = (int)(screenHeight * TopMargin / 100.0 * dpiScale);
                    right = (int)(screenWidth * RightMargin / 100.0 * dpiScale);
                    bottom = (int)(screenHeight * BottomMargin / 100.0 * dpiScale);
                    break;

                case MarginType.GoldenRatio:
                    var ratio = GoldenRatioMode ? 1.618 : CustomRatio;
                    var totalWidth = screenWidth / ratio;
                    var totalHeight = screenHeight / ratio;
                    var marginX = (int)((screenWidth - totalWidth) / 2 * dpiScale);
                    var marginY = (int)((screenHeight - totalHeight) / 2 * dpiScale);
                    left = right = marginX;
                    top = bottom = marginY;
                    break;

                case MarginType.Adaptive:
                    var windowCount = GetEstimatedWindowCount();
                    var adaptiveFactor = Math.Max(0.5, Math.Min(2.0, windowCount / 5.0));
                    left = (int)(LeftMargin * adaptiveFactor * dpiScale);
                    top = (int)(TopMargin * adaptiveFactor * dpiScale);
                    right = (int)(RightMargin * adaptiveFactor * dpiScale);
                    bottom = (int)(BottomMargin * adaptiveFactor * dpiScale);
                    break;

                default: // Fixed
                    left = (int)(LeftMargin * dpiScale);
                    top = (int)(TopMargin * dpiScale);
                    right = (int)(RightMargin * dpiScale);
                    bottom = (int)(BottomMargin * dpiScale);
                    break;
            }

            return (left, top, right, bottom);
        }

        private int GetEstimatedWindowCount()
        {
            try
            {
                var windowManager = new WindowManager();
                return windowManager.GetTopLevelWindows().Count;
            }
            catch
            {
                return 5; // Default estimate
            }
        }

        public void SetUniformMargin(int margin)
        {
            LeftMargin = TopMargin = RightMargin = BottomMargin = margin;
        }

        public void SetHorizontalMargins(int margin)
        {
            LeftMargin = RightMargin = margin;
        }

        public void SetVerticalMargins(int margin)
        {
            TopMargin = BottomMargin = margin;
        }

        public bool IsValid()
        {
            return LeftMargin >= 0 && TopMargin >= 0 && RightMargin >= 0 && BottomMargin >= 0 &&
                   AnimationDuration >= 0 && AnimationDuration <= 5000 &&
                   CustomRatio > 0 && SnapThreshold >= 0;
        }
    }

    public enum MarginType
    {
        Fixed,
        Percentage,
        Adaptive,
        GoldenRatio,
        Dynamic
    }
}
