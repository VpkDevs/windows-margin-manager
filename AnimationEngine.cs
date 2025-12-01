using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public class AnimationEngine
    {
        private readonly Dictionary<IntPtr, AnimationState> activeAnimations;
        private readonly System.Threading.Timer animationTimer;
        private readonly AnimationSettings settings;
        private readonly AnimationStatistics statistics;
        private readonly Queue<AnimationFrame> frameHistory;
        private const int ANIMATION_FPS = 60;
        private const int ANIMATION_INTERVAL = 1000 / ANIMATION_FPS;
        private const int MAX_FRAME_HISTORY = 300; // 5 seconds at 60fps

        public event EventHandler<AnimationEventArgs>? AnimationStarted;
        public event EventHandler<AnimationEventArgs>? AnimationCompleted;
        public event EventHandler<AnimationEventArgs>? AnimationCancelled;

        public AnimationEngine(AnimationSettings? customSettings = null)
        {
            activeAnimations = new Dictionary<IntPtr, AnimationState>();
            settings = customSettings ?? new AnimationSettings();
            statistics = new AnimationStatistics();
            frameHistory = new Queue<AnimationFrame>();
            animationTimer = new System.Threading.Timer(UpdateAnimations, null, Timeout.Infinite, ANIMATION_INTERVAL);
        }

        public async Task AnimateWindowAsync(IntPtr windowHandle, Rectangle startBounds, Rectangle endBounds, 
            int durationMs = 300, EasingType easing = EasingType.EaseOutCubic, AnimationOptions? options = null)
        {
            if (!settings.EnableAnimations) 
            {
                SetWindowPosition(windowHandle, endBounds);
                return;
            }

            if (activeAnimations.ContainsKey(windowHandle))
            {
                var existingAnimation = activeAnimations[windowHandle];
                existingAnimation.IsCancelled = true;
                AnimationCancelled?.Invoke(this, new AnimationEventArgs { WindowHandle = windowHandle, Animation = existingAnimation });
            }

            var effectiveDuration = Math.Max(settings.MinDuration, Math.Min(settings.MaxDuration, durationMs));
            var animationState = new AnimationState
            {
                WindowHandle = windowHandle,
                StartBounds = startBounds,
                EndBounds = endBounds,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMilliseconds(effectiveDuration),
                EasingFunction = GetEasingFunction(easing),
                IsActive = true,
                Options = options ?? new AnimationOptions(),
                Id = Guid.NewGuid()
            };

            activeAnimations[windowHandle] = animationState;
            statistics.TotalAnimationsStarted++;
            
            AnimationStarted?.Invoke(this, new AnimationEventArgs { WindowHandle = windowHandle, Animation = animationState });
            
            if (activeAnimations.Count == 1)
            {
                animationTimer.Change(0, ANIMATION_INTERVAL);
            }

            var tcs = new TaskCompletionSource<bool>();
            animationState.CompletionSource = tcs;

            await tcs.Task;
        }

        public async Task AnimateWindowsSimultaneouslyAsync(Dictionary<IntPtr, (Rectangle start, Rectangle end)> windowAnimations,
            int durationMs = 300, EasingType easing = EasingType.EaseOutCubic, AnimationOptions? options = null)
        {
            if (!settings.EnableAnimations)
            {
                foreach (var kvp in windowAnimations)
                {
                    SetWindowPosition(kvp.Key, kvp.Value.end);
                }
                return;
            }

            var startTime = DateTime.UtcNow;
            var easingFunction = GetEasingFunction(easing);
            var effectiveDuration = Math.Max(settings.MinDuration, Math.Min(settings.MaxDuration, durationMs));
            var completionSources = new List<TaskCompletionSource<bool>>();

            foreach (var kvp in windowAnimations)
            {
                if (activeAnimations.ContainsKey(kvp.Key))
                {
                    var existingAnimation = activeAnimations[kvp.Key];
                    existingAnimation.IsCancelled = true;
                    AnimationCancelled?.Invoke(this, new AnimationEventArgs { WindowHandle = kvp.Key, Animation = existingAnimation });
                }

                var tcs = new TaskCompletionSource<bool>();
                completionSources.Add(tcs);

                var animationState = new AnimationState
                {
                    WindowHandle = kvp.Key,
                    StartBounds = kvp.Value.start,
                    EndBounds = kvp.Value.end,
                    StartTime = startTime,
                    Duration = TimeSpan.FromMilliseconds(effectiveDuration),
                    EasingFunction = easingFunction,
                    IsActive = true,
                    Options = options ?? new AnimationOptions(),
                    CompletionSource = tcs,
                    Id = Guid.NewGuid()
                };

                activeAnimations[kvp.Key] = animationState;
                statistics.TotalAnimationsStarted++;
                AnimationStarted?.Invoke(this, new AnimationEventArgs { WindowHandle = kvp.Key, Animation = animationState });
            }

            if (activeAnimations.Count > 0)
            {
                animationTimer.Change(0, ANIMATION_INTERVAL);
            }

            await Task.WhenAll(completionSources.Select(tcs => tcs.Task));
        }

        public void AnimateWindowsSimultaneously(Dictionary<IntPtr, (Rectangle start, Rectangle end)> windowAnimations,
            int durationMs = 300, EasingType easing = EasingType.EaseOutCubic, AnimationOptions? options = null)
        {
            _ = AnimateWindowsSimultaneouslyAsync(windowAnimations, durationMs, easing, options);
        }

        private void UpdateAnimations(object? state)
        {
            var frameStart = DateTime.UtcNow;
            var completedAnimations = new List<IntPtr>();
            var frameData = new AnimationFrame { Timestamp = frameStart, ActiveAnimations = activeAnimations.Count };

            foreach (var kvp in activeAnimations)
            {
                var animation = kvp.Value;
                
                if (animation.IsCancelled || !animation.IsActive)
                {
                    if (!animation.CompletionHandled)
                    {
                        animation.CompletionSource?.SetResult(false);
                        animation.CompletionHandled = true;
                        statistics.TotalAnimationsCancelled++;
                    }
                    completedAnimations.Add(kvp.Key);
                    continue;
                }

                var elapsed = frameStart - animation.StartTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / animation.Duration.TotalMilliseconds);

                if (progress >= 1.0)
                {
                    var finalBounds = ApplyAnimationOptions(animation.EndBounds, animation.Options);
                    SetWindowPosition(animation.WindowHandle, finalBounds);
                    
                    if (!animation.CompletionHandled)
                    {
                        animation.CompletionSource?.SetResult(true);
                        animation.CompletionHandled = true;
                        statistics.TotalAnimationsCompleted++;
                        AnimationCompleted?.Invoke(this, new AnimationEventArgs { WindowHandle = animation.WindowHandle, Animation = animation });
                    }
                    completedAnimations.Add(kvp.Key);
                }
                else
                {
                    var easedProgress = animation.EasingFunction(progress);
                    var currentBounds = InterpolateBounds(animation.StartBounds, animation.EndBounds, easedProgress);
                    currentBounds = ApplyAnimationOptions(currentBounds, animation.Options);
                    SetWindowPosition(animation.WindowHandle, currentBounds);
                    frameData.WindowsAnimated++;
                }
            }

            foreach (var handle in completedAnimations)
            {
                activeAnimations.Remove(handle);
            }

            var frameEnd = DateTime.UtcNow;
            frameData.ProcessingTime = frameEnd - frameStart;
            RecordFrameData(frameData);

            if (activeAnimations.Count == 0)
            {
                animationTimer.Change(Timeout.Infinite, ANIMATION_INTERVAL);
            }
        }

        private Rectangle InterpolateBounds(Rectangle start, Rectangle end, double progress)
        {
            return new Rectangle(
                (int)(start.X + (end.X - start.X) * progress),
                (int)(start.Y + (end.Y - start.Y) * progress),
                (int)(start.Width + (end.Width - start.Width) * progress),
                (int)(start.Height + (end.Height - start.Height) * progress)
            );
        }

        private Rectangle ApplyAnimationOptions(Rectangle bounds, AnimationOptions options)
        {
            if (options.RoundToPixels)
            {
                bounds = new Rectangle(
                    (int)Math.Round((double)bounds.X),
                    (int)Math.Round((double)bounds.Y),
                    (int)Math.Round((double)bounds.Width),
                    (int)Math.Round((double)bounds.Height)
                );
            }

            if (options.SnapToGrid > 0)
            {
                bounds = new Rectangle(
                    (bounds.X / options.SnapToGrid) * options.SnapToGrid,
                    (bounds.Y / options.SnapToGrid) * options.SnapToGrid,
                    bounds.Width,
                    bounds.Height
                );
            }

            return bounds;
        }

        private void RecordFrameData(AnimationFrame frame)
        {
            frameHistory.Enqueue(frame);
            while (frameHistory.Count > MAX_FRAME_HISTORY)
            {
                frameHistory.Dequeue();
            }

            statistics.TotalFramesProcessed++;
            statistics.AverageProcessingTime = TimeSpan.FromTicks(
                (long)(statistics.AverageProcessingTime.Ticks * 0.9 + frame.ProcessingTime.Ticks * 0.1));
        }

        private void SetWindowPosition(IntPtr windowHandle, Rectangle bounds)
        {
            WindowManager.SetWindowPos(windowHandle, IntPtr.Zero, bounds.X, bounds.Y, 
                bounds.Width, bounds.Height, WindowManager.SWP_NOZORDER | WindowManager.SWP_NOACTIVATE);
        }

        private Func<double, double> GetEasingFunction(EasingType easing)
        {
            return easing switch
            {
                EasingType.Linear => t => t,
                EasingType.EaseInQuad => t => t * t,
                EasingType.EaseOutQuad => t => t * (2 - t),
                EasingType.EaseInOutQuad => t => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t,
                EasingType.EaseInCubic => t => t * t * t,
                EasingType.EaseOutCubic => t => (--t) * t * t + 1,
                EasingType.EaseInOutCubic => t => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,
                EasingType.EaseInQuart => t => t * t * t * t,
                EasingType.EaseOutQuart => t => 1 - (--t) * t * t * t,
                EasingType.EaseInOutQuart => t => t < 0.5 ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t,
                EasingType.EaseInBounce => t => 1 - EaseOutBounce(1 - t),
                EasingType.EaseOutBounce => EaseOutBounce,
                EasingType.EaseInOutBounce => t => t < 0.5 ? (1 - EaseOutBounce(1 - 2 * t)) / 2 : (1 + EaseOutBounce(2 * t - 1)) / 2,
                EasingType.EaseInElastic => EaseInElastic,
                EasingType.EaseOutElastic => EaseOutElastic,
                EasingType.EaseInOutElastic => t => t < 0.5 ? EaseInElastic(2 * t) / 2 : 1 - EaseInElastic(2 * (1 - t)) / 2,
                EasingType.EaseInBack => EaseInBack,
                EasingType.EaseOutBack => EaseOutBack,
                EasingType.EaseInOutBack => t => t < 0.5 ? EaseInBack(2 * t) / 2 : 1 - EaseInBack(2 * (1 - t)) / 2,
                EasingType.EaseInCirc => t => 1 - Math.Sqrt(1 - t * t),
                EasingType.EaseOutCirc => t => Math.Sqrt(1 - (t - 1) * (t - 1)),
                EasingType.EaseInOutCirc => t => t < 0.5 ? (1 - Math.Sqrt(1 - 4 * t * t)) / 2 : (Math.Sqrt(1 - (-2 * t + 2) * (-2 * t + 2)) + 1) / 2,
                EasingType.EaseInExpo => t => t == 0 ? 0 : Math.Pow(2, 10 * (t - 1)),
                EasingType.EaseOutExpo => t => t == 1 ? 1 : 1 - Math.Pow(2, -10 * t),
                EasingType.EaseInOutExpo => t => t == 0 ? 0 : t == 1 ? 1 : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2 : (2 - Math.Pow(2, -20 * t + 10)) / 2,
                EasingType.EaseInSine => t => 1 - Math.Cos(t * Math.PI / 2),
                EasingType.EaseOutSine => t => Math.Sin(t * Math.PI / 2),
                EasingType.EaseInOutSine => t => -(Math.Cos(Math.PI * t) - 1) / 2,
                _ => t => t
            };
        }

        private double EaseInElastic(double t)
        {
            const double c4 = (2 * Math.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : -Math.Pow(2, 10 * t - 10) * Math.Sin((t * 10 - 10.75) * c4);
        }

        private double EaseOutElastic(double t)
        {
            const double c4 = (2 * Math.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
        }

        private double EaseInBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            return c3 * t * t * t - c1 * t * t;
        }

        private double EaseOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
        }

        private double EaseOutBounce(double t)
        {
            if (t < 1 / 2.75)
            {
                return 7.5625 * t * t;
            }
            else if (t < 2 / 2.75)
            {
                return 7.5625 * (t -= 1.5 / 2.75) * t + 0.75;
            }
            else if (t < 2.5 / 2.75)
            {
                return 7.5625 * (t -= 2.25 / 2.75) * t + 0.9375;
            }
            else
            {
                return 7.5625 * (t -= 2.625 / 2.75) * t + 0.984375;
            }
        }

        public void CancelAnimation(IntPtr windowHandle)
        {
            if (activeAnimations.TryGetValue(windowHandle, out var animation))
            {
                animation.IsCancelled = true;
                if (!animation.CompletionHandled)
                {
                    animation.CompletionSource?.SetResult(false);
                    animation.CompletionHandled = true;
                    statistics.TotalAnimationsCancelled++;
                    AnimationCancelled?.Invoke(this, new AnimationEventArgs { WindowHandle = windowHandle, Animation = animation });
                }
            }
        }

        public void CancelAllAnimations()
        {
            foreach (var animation in activeAnimations.Values)
            {
                animation.IsCancelled = true;
                if (!animation.CompletionHandled)
                {
                    animation.CompletionSource?.SetResult(false);
                    animation.CompletionHandled = true;
                    statistics.TotalAnimationsCancelled++;
                    AnimationCancelled?.Invoke(this, new AnimationEventArgs { WindowHandle = animation.WindowHandle, Animation = animation });
                }
            }
            activeAnimations.Clear();
            animationTimer.Change(Timeout.Infinite, ANIMATION_INTERVAL);
        }

        public bool IsAnimating(IntPtr windowHandle)
        {
            return activeAnimations.ContainsKey(windowHandle) && activeAnimations[windowHandle].IsActive;
        }

        public bool IsAnimating() => activeAnimations.Any(kvp => kvp.Value.IsActive);

        public int GetActiveAnimationCount() => activeAnimations.Count(kvp => kvp.Value.IsActive);

        public List<IntPtr> GetAnimatingWindows() => activeAnimations.Where(kvp => kvp.Value.IsActive).Select(kvp => kvp.Key).ToList();

        public AnimationStatistics GetStatistics() => statistics.Clone();

        public List<AnimationFrame> GetFrameHistory() => frameHistory.ToList();

        public void UpdateSettings(AnimationSettings newSettings)
        {
            settings.CopyFrom(newSettings);
        }

        public AnimationSettings GetSettings() => settings.Clone();

        public async Task AnimateWithPresetAsync(IntPtr windowHandle, Rectangle startBounds, Rectangle endBounds, AnimationPreset preset)
        {
            var options = new AnimationOptions
            {
                RoundToPixels = preset.RoundToPixels,
                SnapToGrid = preset.SnapToGrid
            };

            await AnimateWindowAsync(windowHandle, startBounds, endBounds, preset.Duration, preset.Easing, options);
        }

        public void Dispose()
        {
            CancelAllAnimations();
            animationTimer?.Dispose();
        }
    }

    public class AnimationState
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public IntPtr WindowHandle { get; set; }
        public Rectangle StartBounds { get; set; }
        public Rectangle EndBounds { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Func<double, double> EasingFunction { get; set; } = t => t;
        public bool IsActive { get; set; }
        public bool IsCancelled { get; set; }
        public bool CompletionHandled { get; set; }
        public AnimationOptions Options { get; set; } = new AnimationOptions();
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum EasingType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine
    }

    public class AnimationOptions
    {
        public bool RoundToPixels { get; set; } = true;
        public int SnapToGrid { get; set; } = 0;
        public bool PreserveAspectRatio { get; set; } = false;
        public bool RespectMinimumSize { get; set; } = true;
        public Rectangle? ClampToBounds { get; set; }
        public double OpacityStart { get; set; } = 1.0;
        public double OpacityEnd { get; set; } = 1.0;
        public bool EnableShadowEffect { get; set; } = false;
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }

    public class AnimationSettings
    {
        public bool EnableAnimations { get; set; } = true;
        public int DefaultDuration { get; set; } = 300;
        public int MinDuration { get; set; } = 50;
        public int MaxDuration { get; set; } = 2000;
        public EasingType DefaultEasing { get; set; } = EasingType.EaseOutCubic;
        public bool EnablePerformanceMode { get; set; } = false;
        public int MaxSimultaneousAnimations { get; set; } = 50;
        public bool EnableStatistics { get; set; } = true;
        public bool RoundToPixels { get; set; } = true;

        public AnimationSettings Clone()
        {
            return new AnimationSettings
            {
                EnableAnimations = EnableAnimations,
                DefaultDuration = DefaultDuration,
                MinDuration = MinDuration,
                MaxDuration = MaxDuration,
                DefaultEasing = DefaultEasing,
                EnablePerformanceMode = EnablePerformanceMode,
                MaxSimultaneousAnimations = MaxSimultaneousAnimations,
                EnableStatistics = EnableStatistics,
                RoundToPixels = RoundToPixels
            };
        }

        public void CopyFrom(AnimationSettings other)
        {
            EnableAnimations = other.EnableAnimations;
            DefaultDuration = other.DefaultDuration;
            MinDuration = other.MinDuration;
            MaxDuration = other.MaxDuration;
            DefaultEasing = other.DefaultEasing;
            EnablePerformanceMode = other.EnablePerformanceMode;
            MaxSimultaneousAnimations = other.MaxSimultaneousAnimations;
            EnableStatistics = other.EnableStatistics;
            RoundToPixels = other.RoundToPixels;
        }
    }

    public class AnimationStatistics
    {
        public int TotalAnimationsStarted { get; set; }
        public int TotalAnimationsCompleted { get; set; }
        public int TotalAnimationsCancelled { get; set; }
        public long TotalFramesProcessed { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public DateTime LastReset { get; set; } = DateTime.UtcNow;

        public double CompletionRate => TotalAnimationsStarted > 0 ? (double)TotalAnimationsCompleted / TotalAnimationsStarted : 0;
        public double CancellationRate => TotalAnimationsStarted > 0 ? (double)TotalAnimationsCancelled / TotalAnimationsStarted : 0;

        public AnimationStatistics Clone()
        {
            return new AnimationStatistics
            {
                TotalAnimationsStarted = TotalAnimationsStarted,
                TotalAnimationsCompleted = TotalAnimationsCompleted,
                TotalAnimationsCancelled = TotalAnimationsCancelled,
                TotalFramesProcessed = TotalFramesProcessed,
                AverageProcessingTime = AverageProcessingTime,
                LastReset = LastReset
            };
        }

        public void Reset()
        {
            TotalAnimationsStarted = 0;
            TotalAnimationsCompleted = 0;
            TotalAnimationsCancelled = 0;
            TotalFramesProcessed = 0;
            AverageProcessingTime = TimeSpan.Zero;
            LastReset = DateTime.UtcNow;
        }
    }

    public class AnimationFrame
    {
        public DateTime Timestamp { get; set; }
        public int ActiveAnimations { get; set; }
        public int WindowsAnimated { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class AnimationEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public AnimationState Animation { get; set; } = new AnimationState();
    }

    public class AnimationPreset
    {
        public string Name { get; set; } = string.Empty;
        public int Duration { get; set; } = 300;
        public EasingType Easing { get; set; } = EasingType.EaseOutCubic;
        public bool RoundToPixels { get; set; } = true;
        public int SnapToGrid { get; set; } = 0;
        public string Description { get; set; } = string.Empty;

        public static Dictionary<string, AnimationPreset> GetBuiltInPresets()
        {
            return new Dictionary<string, AnimationPreset>
            {
                ["Fast"] = new AnimationPreset { Name = "Fast", Duration = 150, Easing = EasingType.EaseOutQuad, Description = "Quick and snappy animations" },
                ["Normal"] = new AnimationPreset { Name = "Normal", Duration = 300, Easing = EasingType.EaseOutCubic, Description = "Balanced speed and smoothness" },
                ["Smooth"] = new AnimationPreset { Name = "Smooth", Duration = 500, Easing = EasingType.EaseInOutCubic, Description = "Smooth and elegant transitions" },
                ["Bouncy"] = new AnimationPreset { Name = "Bouncy", Duration = 600, Easing = EasingType.EaseOutBounce, Description = "Playful bounce effect" },
                ["Elastic"] = new AnimationPreset { Name = "Elastic", Duration = 800, Easing = EasingType.EaseOutElastic, Description = "Elastic spring effect" },
                ["Instant"] = new AnimationPreset { Name = "Instant", Duration = 0, Easing = EasingType.Linear, Description = "No animation, instant positioning" }
            };
        }
    }
}
