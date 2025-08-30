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
        private readonly Timer animationTimer;
        private const int ANIMATION_FPS = 60;
        private const int ANIMATION_INTERVAL = 1000 / ANIMATION_FPS;

        public AnimationEngine()
        {
            activeAnimations = new Dictionary<IntPtr, AnimationState>();
            animationTimer = new Timer(UpdateAnimations, null, Timeout.Infinite, ANIMATION_INTERVAL);
        }

        public async Task AnimateWindowAsync(IntPtr windowHandle, Rectangle startBounds, Rectangle endBounds, 
            int durationMs = 300, EasingType easing = EasingType.EaseOutCubic)
        {
            if (activeAnimations.ContainsKey(windowHandle))
            {
                activeAnimations[windowHandle].IsCancelled = true;
            }

            var animationState = new AnimationState
            {
                WindowHandle = windowHandle,
                StartBounds = startBounds,
                EndBounds = endBounds,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = GetEasingFunction(easing),
                IsActive = true
            };

            activeAnimations[windowHandle] = animationState;
            
            if (activeAnimations.Count == 1)
            {
                animationTimer.Change(0, ANIMATION_INTERVAL);
            }

            var tcs = new TaskCompletionSource<bool>();
            animationState.CompletionSource = tcs;

            await tcs.Task;
        }

        public void AnimateWindowsSimultaneously(Dictionary<IntPtr, (Rectangle start, Rectangle end)> windowAnimations,
            int durationMs = 300, EasingType easing = EasingType.EaseOutCubic)
        {
            var startTime = DateTime.UtcNow;
            var easingFunction = GetEasingFunction(easing);

            foreach (var kvp in windowAnimations)
            {
                if (activeAnimations.ContainsKey(kvp.Key))
                {
                    activeAnimations[kvp.Key].IsCancelled = true;
                }

                var animationState = new AnimationState
                {
                    WindowHandle = kvp.Key,
                    StartBounds = kvp.Value.start,
                    EndBounds = kvp.Value.end,
                    StartTime = startTime,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    EasingFunction = easingFunction,
                    IsActive = true
                };

                activeAnimations[kvp.Key] = animationState;
            }

            if (activeAnimations.Count > 0)
            {
                animationTimer.Change(0, ANIMATION_INTERVAL);
            }
        }

        private void UpdateAnimations(object? state)
        {
            var currentTime = DateTime.UtcNow;
            var completedAnimations = new List<IntPtr>();

            foreach (var kvp in activeAnimations)
            {
                var animation = kvp.Value;
                
                if (animation.IsCancelled || !animation.IsActive)
                {
                    completedAnimations.Add(kvp.Key);
                    continue;
                }

                var elapsed = currentTime - animation.StartTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / animation.Duration.TotalMilliseconds);

                if (progress >= 1.0)
                {
                    SetWindowPosition(animation.WindowHandle, animation.EndBounds);
                    animation.CompletionSource?.SetResult(true);
                    completedAnimations.Add(kvp.Key);
                }
                else
                {
                    var easedProgress = animation.EasingFunction(progress);
                    var currentBounds = InterpolateBounds(animation.StartBounds, animation.EndBounds, easedProgress);
                    SetWindowPosition(animation.WindowHandle, currentBounds);
                }
            }

            foreach (var handle in completedAnimations)
            {
                activeAnimations.Remove(handle);
            }

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
                _ => t => t
            };
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
                animation.CompletionSource?.SetResult(false);
            }
        }

        public void CancelAllAnimations()
        {
            foreach (var animation in activeAnimations.Values)
            {
                animation.IsCancelled = true;
                animation.CompletionSource?.SetResult(false);
            }
            activeAnimations.Clear();
            animationTimer.Change(Timeout.Infinite, ANIMATION_INTERVAL);
        }

        public bool IsAnimating(IntPtr windowHandle)
        {
            return activeAnimations.ContainsKey(windowHandle) && activeAnimations[windowHandle].IsActive;
        }

        public void Dispose()
        {
            CancelAllAnimations();
            animationTimer?.Dispose();
        }
    }

    public class AnimationState
    {
        public IntPtr WindowHandle { get; set; }
        public Rectangle StartBounds { get; set; }
        public Rectangle EndBounds { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Func<double, double> EasingFunction { get; set; } = t => t;
        public bool IsActive { get; set; }
        public bool IsCancelled { get; set; }
        public TaskCompletionSource<bool>? CompletionSource { get; set; }
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
        EaseInOutBounce
    }
}
