using System;
using System.Collections.Generic;
using System.Drawing;

namespace WindowsMarginManager
{
    public class AdaptiveMarginCalculator
    {
        public SmartMarginSettings CalculateAdaptiveMargins(MonitorInfo monitor, WindowInfo window, MarginSettings baseSettings)
        {
            var adaptiveSettings = new SmartMarginSettings
            {
                LeftMargin = baseSettings.LeftMargin,
                TopMargin = baseSettings.TopMargin,
                RightMargin = baseSettings.RightMargin,
                BottomMargin = baseSettings.BottomMargin,
                UsePercentage = baseSettings.UsePercentage,
                MarginType = baseSettings.MarginType
            };

            var factors = CalculateAdaptiveFactors(monitor, window);
            
            var resolutionMultiplier = CalculateResolutionMultiplier(monitor);
            
            var dpiMultiplier = CalculateDpiMultiplier(monitor);
            
            var contentMultiplier = CalculateContentMultiplier(window);
            
            var aspectRatioMultiplier = CalculateAspectRatioMultiplier(monitor);

            adaptiveSettings.AdaptiveMultiplier = resolutionMultiplier * dpiMultiplier * contentMultiplier * aspectRatioMultiplier;

            adaptiveSettings.LeftMargin = (int)(baseSettings.LeftMargin * adaptiveSettings.AdaptiveMultiplier);
            adaptiveSettings.TopMargin = (int)(baseSettings.TopMargin * adaptiveSettings.AdaptiveMultiplier);
            adaptiveSettings.RightMargin = (int)(baseSettings.RightMargin * adaptiveSettings.AdaptiveMultiplier);
            adaptiveSettings.BottomMargin = (int)(baseSettings.BottomMargin * adaptiveSettings.AdaptiveMultiplier);

            var maxMargin = Math.Min(monitor.Bounds.Width, monitor.Bounds.Height) / 4;
            adaptiveSettings.LeftMargin = Math.Min(adaptiveSettings.LeftMargin, maxMargin);
            adaptiveSettings.TopMargin = Math.Min(adaptiveSettings.TopMargin, maxMargin);
            adaptiveSettings.RightMargin = Math.Min(adaptiveSettings.RightMargin, maxMargin);
            adaptiveSettings.BottomMargin = Math.Min(adaptiveSettings.BottomMargin, maxMargin);

            adaptiveSettings.CalculationMethod = "Adaptive Multi-Factor";
            adaptiveSettings.CalculationParameters = new Dictionary<string, object>
            {
                ["ResolutionMultiplier"] = resolutionMultiplier,
                ["DpiMultiplier"] = dpiMultiplier,
                ["ContentMultiplier"] = contentMultiplier,
                ["AspectRatioMultiplier"] = aspectRatioMultiplier,
                ["FinalMultiplier"] = adaptiveSettings.AdaptiveMultiplier,
                ["MonitorResolution"] = $"{monitor.Bounds.Width}x{monitor.Bounds.Height}",
                ["MonitorDpi"] = monitor.DpiScale,
                ["WindowProcess"] = window.ProcessName
            };

            return adaptiveSettings;
        }

        private Dictionary<string, double> CalculateAdaptiveFactors(MonitorInfo monitor, WindowInfo window)
        {
            return new Dictionary<string, double>
            {
                ["Resolution"] = CalculateResolutionMultiplier(monitor),
                ["DPI"] = CalculateDpiMultiplier(monitor),
                ["Content"] = CalculateContentMultiplier(window),
                ["AspectRatio"] = CalculateAspectRatioMultiplier(monitor),
                ["Usage"] = CalculateUsageMultiplier(window)
            };
        }

        private double CalculateResolutionMultiplier(MonitorInfo monitor)
        {
            var resolution = monitor.Bounds.Width * monitor.Bounds.Height;
            
            var baseResolution = 1920 * 1080;
            var resolutionRatio = (double)resolution / baseResolution;

            if (resolutionRatio <= 0.5) // Very low resolution
                return 0.6;
            else if (resolutionRatio <= 1.0) // Standard resolution
                return 0.8 + (resolutionRatio * 0.2);
            else if (resolutionRatio <= 2.0) // High resolution
                return 1.0 + ((resolutionRatio - 1.0) * 0.3);
            else if (resolutionRatio <= 4.0) // Very high resolution (4K)
                return 1.3 + ((resolutionRatio - 2.0) * 0.2);
            else // Ultra high resolution (8K+)
                return 1.7 + ((resolutionRatio - 4.0) * 0.1);
        }

        private double CalculateDpiMultiplier(MonitorInfo monitor)
        {
            var dpiScale = monitor.DpiScale;
            
            if (dpiScale <= 1.0)
                return 1.0;
            else if (dpiScale <= 1.25)
                return 1.0 + ((dpiScale - 1.0) * 0.4); // 125% scaling
            else if (dpiScale <= 1.5)
                return 1.1 + ((dpiScale - 1.25) * 0.6); // 150% scaling
            else if (dpiScale <= 2.0)
                return 1.25 + ((dpiScale - 1.5) * 0.5); // 200% scaling
            else
                return 1.5 + ((dpiScale - 2.0) * 0.25); // Higher scaling
        }

        private double CalculateContentMultiplier(WindowInfo window)
        {
            var processName = window.ProcessName.ToLower();
            
            if (IsProductivityApp(processName))
                return 1.2; // More margins for focus
            else if (IsMediaApp(processName))
                return 0.7; // Less margins for immersion
            else if (IsUtilityApp(processName))
                return 0.9; // Moderate margins
            else if (IsGameApp(processName))
                return 0.5; // Minimal margins
            else if (IsBrowserApp(processName))
                return 1.0; // Standard margins
            else if (IsCodeEditorApp(processName))
                return 1.1; // Slightly more margins
            else
                return 1.0; // Default
        }

        private double CalculateAspectRatioMultiplier(MonitorInfo monitor)
        {
            var aspectRatio = (double)monitor.Bounds.Width / monitor.Bounds.Height;
            
            if (Math.Abs(aspectRatio - 1.333) < 0.1) // 4:3
                return 1.1;
            else if (Math.Abs(aspectRatio - 1.6) < 0.1) // 16:10
                return 1.05;
            else if (Math.Abs(aspectRatio - 1.778) < 0.1) // 16:9
                return 1.0;
            else if (Math.Abs(aspectRatio - 2.333) < 0.1) // 21:9 ultrawide
                return 0.8;
            else if (aspectRatio > 2.5) // Super ultrawide
                return 0.7;
            else
                return 1.0; // Default for other ratios
        }

        private double CalculateUsageMultiplier(WindowInfo window)
        {
            return 1.0;
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

        private bool IsUtilityApp(string processName)
        {
            var utilityApps = new[] { "explorer", "taskmgr", "regedit", "cmd", "powershell", "calculator", "notepad" };
            return utilityApps.Any(app => processName.Contains(app));
        }

        private bool IsGameApp(string processName)
        {
            var gameApps = new[] { "steam", "origin", "uplay", "epic", "battle", "minecraft", "wow", "lol" };
            return gameApps.Any(app => processName.Contains(app));
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
}
