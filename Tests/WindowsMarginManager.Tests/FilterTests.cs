using System;
using Xunit;
using WindowsMarginManager;

namespace WindowsMarginManager.Tests
{
    /// <summary>
    /// Unit tests for the Range and TimeRange structs.
    /// </summary>
    public class RangeTests
    {
        [Fact]
        public void Range_Constructor_SetsMinMax()
        {
            var range = new Range<int>(10, 100);
            
            Assert.Equal(10, range.Min);
            Assert.Equal(100, range.Max);
        }

        [Theory]
        [InlineData(5, false)]   // Below min
        [InlineData(10, true)]   // At min
        [InlineData(50, true)]   // In middle
        [InlineData(100, true)]  // At max
        [InlineData(150, false)] // Above max
        public void Range_Contains_ReturnsCorrectResult(int value, bool expected)
        {
            var range = new Range<int>(10, 100);
            Assert.Equal(expected, range.Contains(value));
        }

        [Theory]
        [InlineData(0.5, false)]   // Below min
        [InlineData(1.0, true)]    // At min
        [InlineData(1.5, true)]    // In middle
        [InlineData(2.0, true)]    // At max
        [InlineData(3.0, false)]   // Above max
        public void Range_Double_Contains_ReturnsCorrectResult(double value, bool expected)
        {
            var range = new Range<double>(1.0, 2.0);
            Assert.Equal(expected, range.Contains(value));
        }

        [Fact]
        public void TimeRange_Constructor_SetsStartEnd()
        {
            var start = new TimeSpan(9, 0, 0);  // 9:00 AM
            var end = new TimeSpan(17, 0, 0);   // 5:00 PM
            var range = new TimeRange(start, end);
            
            Assert.Equal(start, range.Start);
            Assert.Equal(end, range.End);
        }

        [Theory]
        [InlineData(8, 0, false)]   // Before start
        [InlineData(9, 0, true)]    // At start
        [InlineData(12, 0, true)]   // In middle
        [InlineData(17, 0, true)]   // At end
        [InlineData(18, 0, false)]  // After end
        public void TimeRange_Contains_NormalRange_ReturnsCorrectResult(int hours, int minutes, bool expected)
        {
            var range = new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
            var time = new TimeSpan(hours, minutes, 0);
            
            Assert.Equal(expected, range.Contains(time));
        }

        [Theory]
        [InlineData(18, 0, true)]   // After start (evening)
        [InlineData(23, 0, true)]   // Late night
        [InlineData(0, 0, true)]    // Midnight (in overnight range)
        [InlineData(5, 0, true)]    // Early morning (before end)
        [InlineData(6, 0, true)]    // At end
        [InlineData(8, 0, false)]   // Outside range
        [InlineData(12, 0, false)]  // Midday (outside)
        public void TimeRange_Contains_OvernightRange_ReturnsCorrectResult(int hours, int minutes, bool expected)
        {
            // Range from 6 PM to 6 AM (overnight)
            var range = new TimeRange(new TimeSpan(18, 0, 0), new TimeSpan(6, 0, 0));
            var time = new TimeSpan(hours, minutes, 0);
            
            Assert.Equal(expected, range.Contains(time));
        }
    }

    /// <summary>
    /// Unit tests for the WindowFilterCriteria class.
    /// </summary>
    public class WindowFilterCriteriaTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var criteria = new WindowFilterCriteria();
            
            Assert.False(criteria.IncludeMinimized);
            Assert.True(criteria.RequireVisibleTitle);
            Assert.False(criteria.CaseSensitive);
            Assert.Equal(FilterMode.Include, criteria.Mode);
            Assert.Equal(0, criteria.Priority);
            Assert.True(criteria.Enabled);
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new WindowFilterCriteria
            {
                Name = "Test Filter",
                TitlePattern = "Test*",
                MinWidth = 800,
                MinHeight = 600,
                ProcessNames = new System.Collections.Generic.List<string> { "chrome", "firefox" },
                CaseSensitive = true,
                Priority = 5
            };
            
            var clone = original.Clone();
            
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.TitlePattern, clone.TitlePattern);
            Assert.Equal(original.MinWidth, clone.MinWidth);
            Assert.Equal(original.MinHeight, clone.MinHeight);
            Assert.Equal(original.CaseSensitive, clone.CaseSensitive);
            Assert.Equal(original.Priority, clone.Priority);
            
            // Verify they are independent
            clone.Name = "Modified";
            Assert.NotEqual(original.Name, clone.Name);
        }

        [Fact]
        public void Clone_CopiesProcessNames()
        {
            var original = new WindowFilterCriteria
            {
                ProcessNames = new System.Collections.Generic.List<string> { "app1", "app2" }
            };
            
            var clone = original.Clone();
            
            Assert.NotNull(clone.ProcessNames);
            Assert.Equal(2, clone.ProcessNames.Count);
            Assert.Contains("app1", clone.ProcessNames);
            Assert.Contains("app2", clone.ProcessNames);
            
            // Verify they are independent
            clone.ProcessNames.Add("app3");
            Assert.Equal(2, original.ProcessNames.Count);
            Assert.Equal(3, clone.ProcessNames.Count);
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            var criteria1 = new WindowFilterCriteria
            {
                TitlePattern = "Test",
                MinWidth = 800,
                CaseSensitive = true
            };
            
            var criteria2 = new WindowFilterCriteria
            {
                TitlePattern = "Test",
                MinWidth = 800,
                CaseSensitive = true
            };
            
            Assert.Equal(criteria1.GetHashCode(), criteria2.GetHashCode());
        }
    }
}
