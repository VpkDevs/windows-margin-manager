using System;
using System.IO;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    /// <summary>
    /// Represents margin settings for window positioning with comprehensive validation.
    /// </summary>
    public class MarginSettings
    {
        private int _leftMargin = 50;
        private int _topMargin = 50;
        private int _rightMargin = 50;
        private int _bottomMargin = 50;
        private int _animationDuration = 300;
        private double _customRatio = 1.618;
        private int _snapThreshold = 20;

        /// <summary>
        /// Gets or sets the left margin. Must be between 0 and 10000.
        /// </summary>
        public int LeftMargin
        {
            get => _leftMargin;
            set => _leftMargin = Math.Clamp(value, 0, 10000);
        }

        /// <summary>
        /// Gets or sets the top margin. Must be between 0 and 10000.
        /// </summary>
        public int TopMargin
        {
            get => _topMargin;
            set => _topMargin = Math.Clamp(value, 0, 10000);
        }

        /// <summary>
        /// Gets or sets the right margin. Must be between 0 and 10000.
        /// </summary>
        public int RightMargin
        {
            get => _rightMargin;
            set => _rightMargin = Math.Clamp(value, 0, 10000);
        }

        /// <summary>
        /// Gets or sets the bottom margin. Must be between 0 and 10000.
        /// </summary>
        public int BottomMargin
        {
            get => _bottomMargin;
            set => _bottomMargin = Math.Clamp(value, 0, 10000);
        }

        /// <summary>
        /// Gets or sets whether margins are specified as percentages.
        /// </summary>
        public bool UsePercentage { get; set; } = false;

        /// <summary>
        /// Gets or sets whether animations are enabled.
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// Gets or sets the animation duration in milliseconds. Must be between 0 and 5000.
        /// </summary>
        public int AnimationDuration
        {
            get => _animationDuration;
            set => _animationDuration = Math.Clamp(value, 0, 5000);
        }

        /// <summary>
        /// Gets or sets the easing type for animations.
        /// </summary>
        public EasingType AnimationEasing { get; set; } = EasingType.EaseOutCubic;

        /// <summary>
        /// Gets or sets the margin calculation type.
        /// </summary>
        public MarginType MarginType { get; set; } = MarginType.Fixed;

        /// <summary>
        /// Gets or sets whether adaptive margins are enabled.
        /// </summary>
        public bool AdaptiveMargins { get; set; } = false;

        /// <summary>
        /// Gets or sets whether golden ratio mode is enabled.
        /// </summary>
        public bool GoldenRatioMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the custom ratio for margin calculations. Must be between 0.1 and 10.
        /// </summary>
        public double CustomRatio
        {
            get => _customRatio;
            set => _customRatio = Math.Clamp(value, 0.1, 10.0);
        }

        /// <summary>
        /// Gets or sets whether minimum window sizes should be respected.
        /// </summary>
        public bool RespectMinimumSizes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether aspect ratio should be preserved during resizing.
        /// </summary>
        public bool PreserveAspectRatio { get; set; } = false;

        /// <summary>
        /// Gets or sets whether collision detection is enabled.
        /// </summary>
        public bool EnableCollisionDetection { get; set; } = true;

        /// <summary>
        /// Gets or sets whether snap zones are enabled.
        /// </summary>
        public bool EnableSnapZones { get; set; } = true;

        /// <summary>
        /// Gets or sets the snap threshold in pixels. Must be between 0 and 200.
        /// </summary>
        public int SnapThreshold
        {
            get => _snapThreshold;
            set => _snapThreshold = Math.Clamp(value, 0, 200);
        }

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

        /// <summary>
        /// Sets a uniform margin value for all sides.
        /// </summary>
        /// <param name="margin">The margin value to apply to all sides.</param>
        public void SetUniformMargin(int margin)
        {
            LeftMargin = TopMargin = RightMargin = BottomMargin = margin;
        }

        /// <summary>
        /// Sets horizontal margins (left and right) to the same value.
        /// </summary>
        /// <param name="margin">The margin value for left and right sides.</param>
        public void SetHorizontalMargins(int margin)
        {
            LeftMargin = RightMargin = margin;
        }

        /// <summary>
        /// Sets vertical margins (top and bottom) to the same value.
        /// </summary>
        /// <param name="margin">The margin value for top and bottom sides.</param>
        public void SetVerticalMargins(int margin)
        {
            TopMargin = BottomMargin = margin;
        }

        /// <summary>
        /// Validates the current margin settings.
        /// </summary>
        /// <returns>True if all settings are valid; otherwise, false.</returns>
        public bool IsValid()
        {
            // Margins are automatically clamped, so just check logical constraints
            if (UsePercentage)
            {
                // For percentage mode, margins should not exceed 50% on each side
                var totalHorizontal = LeftMargin + RightMargin;
                var totalVertical = TopMargin + BottomMargin;
                if (totalHorizontal >= 100 || totalVertical >= 100)
                    return false;
            }

            return AnimationDuration >= 0 && AnimationDuration <= 5000 &&
                   CustomRatio > 0.1 && CustomRatio <= 10.0 &&
                   SnapThreshold >= 0 && SnapThreshold <= 200;
        }

        /// <summary>
        /// Gets a summary of the current margin settings.
        /// </summary>
        /// <returns>A formatted string describing the margin settings.</returns>
        public override string ToString()
        {
            var unit = UsePercentage ? "%" : "px";
            return $"Margins: L:{LeftMargin}{unit} T:{TopMargin}{unit} R:{RightMargin}{unit} B:{BottomMargin}{unit}, " +
                   $"Type: {MarginType}, Animations: {(EnableAnimations ? "On" : "Off")}";
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
