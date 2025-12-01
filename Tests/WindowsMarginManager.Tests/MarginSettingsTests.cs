using System;
using Xunit;
using WindowsMarginManager;

namespace WindowsMarginManager.Tests
{
    /// <summary>
    /// Unit tests for the MarginSettings class.
    /// </summary>
    public class MarginSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Note: We can't test the default constructor as it loads from file
            // Instead, we'll use a copy constructor with known values
            var settings = new MarginSettings(new MarginSettings());
            
            Assert.True(settings.LeftMargin >= 0);
            Assert.True(settings.TopMargin >= 0);
            Assert.True(settings.RightMargin >= 0);
            Assert.True(settings.BottomMargin >= 0);
        }

        [Theory]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(10000, 10000)]
        [InlineData(20000, 10000)] // Clamped to max
        public void LeftMargin_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = input;
            Assert.Equal(expected, settings.LeftMargin);
        }

        [Theory]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(10000, 10000)]
        [InlineData(20000, 10000)] // Clamped to max
        public void TopMargin_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.TopMargin = input;
            Assert.Equal(expected, settings.TopMargin);
        }

        [Theory]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(10000, 10000)]
        [InlineData(20000, 10000)] // Clamped to max
        public void RightMargin_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.RightMargin = input;
            Assert.Equal(expected, settings.RightMargin);
        }

        [Theory]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(10000, 10000)]
        [InlineData(20000, 10000)] // Clamped to max
        public void BottomMargin_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.BottomMargin = input;
            Assert.Equal(expected, settings.BottomMargin);
        }

        [Theory]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        [InlineData(300, 300)]
        [InlineData(5000, 5000)]
        [InlineData(6000, 5000)] // Clamped to max
        public void AnimationDuration_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.AnimationDuration = input;
            Assert.Equal(expected, settings.AnimationDuration);
        }

        [Theory]
        [InlineData(-1.0, 0.1)] // Clamped to min
        [InlineData(0.0, 0.1)]  // Clamped to min
        [InlineData(0.1, 0.1)]
        [InlineData(1.618, 1.618)]
        [InlineData(10.0, 10.0)]
        [InlineData(15.0, 10.0)] // Clamped to max
        public void CustomRatio_ClampsValue(double input, double expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.CustomRatio = input;
            Assert.Equal(expected, settings.CustomRatio, 3);
        }

        [Theory]
        [InlineData(-50, 0)]
        [InlineData(0, 0)]
        [InlineData(20, 20)]
        [InlineData(200, 200)]
        [InlineData(300, 200)] // Clamped to max
        public void SnapThreshold_ClampsValue(int input, int expected)
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.SnapThreshold = input;
            Assert.Equal(expected, settings.SnapThreshold);
        }

        [Fact]
        public void SetUniformMargin_SetsAllMargins()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.SetUniformMargin(75);
            
            Assert.Equal(75, settings.LeftMargin);
            Assert.Equal(75, settings.TopMargin);
            Assert.Equal(75, settings.RightMargin);
            Assert.Equal(75, settings.BottomMargin);
        }

        [Fact]
        public void SetHorizontalMargins_SetsLeftAndRight()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.SetHorizontalMargins(60);
            
            Assert.Equal(60, settings.LeftMargin);
            Assert.Equal(60, settings.RightMargin);
        }

        [Fact]
        public void SetVerticalMargins_SetsTopAndBottom()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.SetVerticalMargins(80);
            
            Assert.Equal(80, settings.TopMargin);
            Assert.Equal(80, settings.BottomMargin);
        }

        [Fact]
        public void IsValid_ValidSettings_ReturnsTrue()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = 50;
            settings.TopMargin = 50;
            settings.RightMargin = 50;
            settings.BottomMargin = 50;
            settings.UsePercentage = false;
            settings.AnimationDuration = 300;
            settings.CustomRatio = 1.618;
            settings.SnapThreshold = 20;
            
            Assert.True(settings.IsValid());
        }

        [Fact]
        public void IsValid_PercentageTotalTooHigh_ReturnsFalse()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.UsePercentage = true;
            settings.LeftMargin = 60; // 60%
            settings.RightMargin = 50; // 50%
            // Total horizontal = 110%, which is invalid
            
            Assert.False(settings.IsValid());
        }

        [Fact]
        public void CopyConstructor_CopiesAllValues()
        {
            var original = new MarginSettings(new MarginSettings());
            original.LeftMargin = 100;
            original.TopMargin = 80;
            original.RightMargin = 60;
            original.BottomMargin = 40;
            original.UsePercentage = true;
            original.EnableAnimations = false;
            original.AnimationDuration = 500;
            original.MarginType = MarginType.Percentage;
            original.CustomRatio = 2.0;
            original.SnapThreshold = 50;
            
            var copy = new MarginSettings(original);
            
            Assert.Equal(original.LeftMargin, copy.LeftMargin);
            Assert.Equal(original.TopMargin, copy.TopMargin);
            Assert.Equal(original.RightMargin, copy.RightMargin);
            Assert.Equal(original.BottomMargin, copy.BottomMargin);
            Assert.Equal(original.UsePercentage, copy.UsePercentage);
            Assert.Equal(original.EnableAnimations, copy.EnableAnimations);
            Assert.Equal(original.AnimationDuration, copy.AnimationDuration);
            Assert.Equal(original.MarginType, copy.MarginType);
            Assert.Equal(original.CustomRatio, copy.CustomRatio);
            Assert.Equal(original.SnapThreshold, copy.SnapThreshold);
        }

        [Fact]
        public void CalculateMargins_FixedType_ReturnsCorrectValues()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = 50;
            settings.TopMargin = 40;
            settings.RightMargin = 30;
            settings.BottomMargin = 20;
            settings.MarginType = MarginType.Fixed;
            
            var (left, top, right, bottom) = settings.CalculateMargins(1920, 1080);
            
            Assert.Equal(50, left);
            Assert.Equal(40, top);
            Assert.Equal(30, right);
            Assert.Equal(20, bottom);
        }

        [Fact]
        public void CalculateMargins_WithDpiScale_ScalesCorrectly()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = 50;
            settings.TopMargin = 50;
            settings.RightMargin = 50;
            settings.BottomMargin = 50;
            settings.MarginType = MarginType.Fixed;
            
            var (left, top, right, bottom) = settings.CalculateMargins(1920, 1080, 1.5);
            
            Assert.Equal(75, left);
            Assert.Equal(75, top);
            Assert.Equal(75, right);
            Assert.Equal(75, bottom);
        }

        [Fact]
        public void CalculateMargins_PercentageType_CalculatesCorrectly()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = 10; // 10%
            settings.TopMargin = 10;
            settings.RightMargin = 10;
            settings.BottomMargin = 10;
            settings.MarginType = MarginType.Percentage;
            
            var (left, top, right, bottom) = settings.CalculateMargins(1000, 1000);
            
            Assert.Equal(100, left);   // 10% of 1000
            Assert.Equal(100, top);
            Assert.Equal(100, right);
            Assert.Equal(100, bottom);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var settings = new MarginSettings(new MarginSettings());
            settings.LeftMargin = 50;
            settings.TopMargin = 50;
            settings.RightMargin = 50;
            settings.BottomMargin = 50;
            settings.UsePercentage = false;
            settings.MarginType = MarginType.Fixed;
            settings.EnableAnimations = true;
            
            var result = settings.ToString();
            
            Assert.Contains("L:50px", result);
            Assert.Contains("T:50px", result);
            Assert.Contains("R:50px", result);
            Assert.Contains("B:50px", result);
            Assert.Contains("Fixed", result);
        }
    }
}
