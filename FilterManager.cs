using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class FilterManager
    {
        private readonly string filtersPath;
        private List<WindowFilterCriteria> customFilters;
        private Dictionary<string, WindowFilterCriteria> builtInFilters;
        private FilterStatistics statistics;

        public event EventHandler<FilterChangedEventArgs>? FilterChanged;

        public FilterManager()
        {
            filtersPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowsMarginManager",
                "filters.json"
            );

            customFilters = new List<WindowFilterCriteria>();
            builtInFilters = FilterPresets.GetBuiltInPresets();
            statistics = new FilterStatistics();
            
            LoadCustomFilters();
        }

        public List<WindowFilterCriteria> GetAllFilters()
        {
            var allFilters = new List<WindowFilterCriteria>();
            allFilters.AddRange(builtInFilters.Values);
            allFilters.AddRange(customFilters);
            return allFilters.Where(f => f.Enabled).OrderBy(f => f.Priority).ToList();
        }

        public List<WindowFilterCriteria> GetCustomFilters() => new List<WindowFilterCriteria>(customFilters);

        public Dictionary<string, WindowFilterCriteria> GetBuiltInFilters() => new Dictionary<string, WindowFilterCriteria>(builtInFilters);

        public WindowFilterCriteria? GetFilter(string name)
        {
            if (builtInFilters.TryGetValue(name, out var builtIn))
                return builtIn;

            return customFilters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void SaveFilter(WindowFilterCriteria filter)
        {
            var existing = customFilters.FirstOrDefault(f => f.Name.Equals(filter.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                customFilters.Remove(existing);
            }

            filter.LastModified = DateTime.UtcNow;
            customFilters.Add(filter);
            
            SaveCustomFilters();
            FilterChanged?.Invoke(this, new FilterChangedEventArgs { Filter = filter, Action = FilterAction.Saved });
        }

        public void DeleteFilter(string name)
        {
            var filter = customFilters.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (filter != null)
            {
                customFilters.Remove(filter);
                SaveCustomFilters();
                FilterChanged?.Invoke(this, new FilterChangedEventArgs { Filter = filter, Action = FilterAction.Deleted });
            }
        }

        public void EnableFilter(string name, bool enabled)
        {
            var filter = GetFilter(name);
            if (filter != null)
            {
                filter.Enabled = enabled;
                filter.LastModified = DateTime.UtcNow;
                
                if (customFilters.Contains(filter))
                {
                    SaveCustomFilters();
                }
                
                FilterChanged?.Invoke(this, new FilterChangedEventArgs 
                { 
                    Filter = filter, 
                    Action = enabled ? FilterAction.Enabled : FilterAction.Disabled 
                });
            }
        }

        public List<WindowInfo> ApplyFilters(List<WindowInfo> windows, params string[] filterNames)
        {
            var filters = filterNames.Select(GetFilter).Where(f => f != null).ToList();
            return ApplyFilters(windows, filters!);
        }

        public List<WindowInfo> ApplyFilters(List<WindowInfo> windows, List<WindowFilterCriteria> filters)
        {
            if (!filters.Any()) return windows;

            var windowFilter = new WindowFilter();
            var filteredWindows = new List<WindowInfo>();

            foreach (var window in windows)
            {
                bool shouldInclude = false;
                bool shouldExclude = false;

                foreach (var filter in filters.Where(f => f.Enabled).OrderBy(f => f.Priority))
                {
                    if (windowFilter.MatchesCriteria(window, filter))
                    {
                        switch (filter.Mode)
                        {
                            case FilterMode.Include:
                                shouldInclude = true;
                                break;
                            case FilterMode.Exclude:
                                shouldExclude = true;
                                break;
                            case FilterMode.Priority:
                                shouldInclude = true;
                                break;
                        }
                    }
                }

                if (shouldInclude && !shouldExclude)
                {
                    filteredWindows.Add(window);
                }
            }

            UpdateStatistics(windows.Count, filteredWindows.Count, filters.Count);
            return filteredWindows;
        }

        public WindowFilterCriteria CreateSmartFilter(List<WindowInfo> sampleWindows, string name)
        {
            var filter = new WindowFilterCriteria
            {
                Name = name,
                Description = $"Smart filter created from {sampleWindows.Count} sample windows",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var processNames = sampleWindows.Select(w => w.ProcessName).Distinct().ToList();
            if (processNames.Count <= 5)
            {
                filter.ProcessNames = processNames;
            }

            var commonTitleWords = ExtractCommonTitleWords(sampleWindows);
            if (commonTitleWords.Any())
            {
                filter.TitleContains = commonTitleWords.Take(3).ToList();
            }

            var avgWidth = (int)sampleWindows.Average(w => w.Rectangle.Width);
            var avgHeight = (int)sampleWindows.Average(w => w.Rectangle.Height);
            
            filter.MinWidth = (int)(avgWidth * 0.7);
            filter.MinHeight = (int)(avgHeight * 0.7);
            filter.MaxWidth = (int)(avgWidth * 1.5);
            filter.MaxHeight = (int)(avgHeight * 1.5);

            var commonStates = sampleWindows.GroupBy(w => w.WindowState)
                .Where(g => g.Count() > sampleWindows.Count * 0.3)
                .Select(g => g.Key).ToList();
            
            if (commonStates.Any())
            {
                filter.WindowStates = commonStates;
            }

            return filter;
        }

        private List<string> ExtractCommonTitleWords(List<WindowInfo> windows)
        {
            var wordCounts = new Dictionary<string, int>();
            
            foreach (var window in windows)
            {
                var words = window.Title.Split(new[] { ' ', '-', '_', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words.Where(w => w.Length > 3))
                {
                    wordCounts[word.ToLower()] = wordCounts.GetValueOrDefault(word.ToLower(), 0) + 1;
                }
            }

            return wordCounts.Where(kvp => kvp.Value > windows.Count * 0.3)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private void LoadCustomFilters()
        {
            try
            {
                if (File.Exists(filtersPath))
                {
                    var json = File.ReadAllText(filtersPath);
                    var loadedFilters = JsonConvert.DeserializeObject<List<WindowFilterCriteria>>(json);
                    if (loadedFilters != null)
                    {
                        customFilters = loadedFilters;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom filters: {ex.Message}");
            }
        }

        private void SaveCustomFilters()
        {
            try
            {
                var directory = Path.GetDirectoryName(filtersPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(customFilters, Formatting.Indented);
                File.WriteAllText(filtersPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom filters: {ex.Message}");
            }
        }

        private void UpdateStatistics(int totalWindows, int filteredWindows, int filtersApplied)
        {
            statistics.TotalFiltersApplied += filtersApplied;
            statistics.WindowsMatched += filteredWindows;
            statistics.WindowsExcluded += totalWindows - filteredWindows;
        }

        public FilterStatistics GetStatistics() => statistics;

        public void ResetStatistics()
        {
            statistics = new FilterStatistics();
        }
    }

    public class FilterChangedEventArgs : EventArgs
    {
        public WindowFilterCriteria Filter { get; set; } = new WindowFilterCriteria();
        public FilterAction Action { get; set; }
    }

    public enum FilterAction
    {
        Saved,
        Deleted,
        Enabled,
        Disabled,
        Applied
    }
}
