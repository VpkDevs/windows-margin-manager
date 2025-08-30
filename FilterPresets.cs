using System;
using System.Collections.Generic;

namespace WindowsMarginManager
{
    public static class FilterPresets
    {
        public static Dictionary<string, WindowFilterCriteria> GetBuiltInPresets()
        {
            return new Dictionary<string, WindowFilterCriteria>
            {
                ["Development"] = new WindowFilterCriteria
                {
                    Name = "Development Tools",
                    Description = "Common development applications",
                    ProcessNames = new List<string>
                    {
                        "devenv", "Code", "rider64", "idea64", "eclipse", "atom",
                        "sublime_text", "notepad++", "vim", "emacs", "WebStorm",
                        "PhpStorm", "PyCharm", "IntelliJ IDEA", "Android Studio"
                    },
                    MinWidth = 800,
                    MinHeight = 600,
                    WindowStates = new List<WindowState> { WindowState.Normal, WindowState.Maximized }
                },

                ["Browsers"] = new WindowFilterCriteria
                {
                    Name = "Web Browsers",
                    Description = "All major web browsers",
                    ProcessNames = new List<string>
                    {
                        "chrome", "firefox", "msedge", "opera", "brave", "safari",
                        "iexplore", "vivaldi", "tor", "waterfox"
                    },
                    MinWidth = 400,
                    MinHeight = 300
                },

                ["Media"] = new WindowFilterCriteria
                {
                    Name = "Media Applications",
                    Description = "Video, audio, and image editing applications",
                    ProcessNames = new List<string>
                    {
                        "vlc", "wmplayer", "spotify", "itunes", "foobar2000",
                        "photoshop", "gimp", "paint.net", "premiere", "afterfx",
                        "audacity", "obs64", "streamlabs obs", "discord", "zoom"
                    }
                },

                ["Gaming"] = new WindowFilterCriteria
                {
                    Name = "Gaming Applications",
                    Description = "Games and gaming platforms",
                    ProcessNames = new List<string>
                    {
                        "steam", "origin", "uplay", "epicgameslauncher", "battle.net",
                        "gog galaxy", "minecraft", "roblox"
                    },
                    TitleContains = new List<string>
                    {
                        "- Steam", "Origin", "Ubisoft Connect", "Epic Games"
                    }
                },

                ["Productivity"] = new WindowFilterCriteria
                {
                    Name = "Productivity Suite",
                    Description = "Office and productivity applications",
                    ProcessNames = new List<string>
                    {
                        "winword", "excel", "powerpnt", "outlook", "onenote",
                        "teams", "slack", "notion", "obsidian", "evernote",
                        "acrobat", "foxit reader"
                    }
                },

                ["System"] = new WindowFilterCriteria
                {
                    Name = "System Applications",
                    Description = "Windows system and utility applications",
                    ProcessNames = new List<string>
                    {
                        "explorer", "taskmgr", "regedit", "cmd", "powershell",
                        "mmc", "services", "eventvwr", "perfmon", "msconfig"
                    },
                    Mode = FilterMode.Exclude
                },

                ["Large Windows"] = new WindowFilterCriteria
                {
                    Name = "Large Windows Only",
                    Description = "Windows larger than 800x600",
                    MinWidth = 800,
                    MinHeight = 600,
                    WindowStates = new List<WindowState> { WindowState.Normal, WindowState.Maximized }
                },

                ["Primary Monitor"] = new WindowFilterCriteria
                {
                    Name = "Primary Monitor Only",
                    Description = "Windows on the primary monitor",
                    PrimaryMonitorOnly = true
                },

                ["Work Hours"] = new WindowFilterCriteria
                {
                    Name = "Work Hours Filter",
                    Description = "Active during business hours on weekdays",
                    TimeRange = new TimeRange(new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
                    DaysOfWeek = new List<DayOfWeek>
                    {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                        DayOfWeek.Thursday, DayOfWeek.Friday
                    }
                },

                ["Ultrawide Optimized"] = new WindowFilterCriteria
                {
                    Name = "Ultrawide Monitor Optimized",
                    Description = "Applications that work well on ultrawide monitors",
                    AspectRatioRange = new Range<double>(2.0, 4.0),
                    ProcessNames = new List<string>
                    {
                        "chrome", "firefox", "code", "devenv", "photoshop",
                        "premiere", "davinci resolve"
                    }
                },

                ["No Dialogs"] = new WindowFilterCriteria
                {
                    Name = "Exclude Dialog Windows",
                    Description = "Excludes small dialog and popup windows",
                    MinWidth = 300,
                    MinHeight = 200,
                    ExcludedTitles = new List<string>
                    {
                        "Properties", "Options", "Settings", "Preferences",
                        "About", "Error", "Warning", "Confirmation"
                    },
                    ExcludedClassNames = new List<string>
                    {
                        "#32770", "Dialog", "MessageBox"
                    }
                },

                ["Recent Windows"] = new WindowFilterCriteria
                {
                    Name = "Recently Active Windows",
                    Description = "Windows that were active in the last 5 minutes",
                    MaxAge = TimeSpan.FromMinutes(5)
                }
            };
        }

        public static WindowFilterCriteria CreateCustomFilter(string name, Action<WindowFilterCriteria> configure)
        {
            var filter = new WindowFilterCriteria
            {
                Name = name,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            
            configure(filter);
            return filter;
        }

        public static WindowFilterCriteria CombineFilters(string name, params WindowFilterCriteria[] filters)
        {
            var combined = new WindowFilterCriteria
            {
                Name = name,
                Description = $"Combined filter from {filters.Length} filters",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var allProcessNames = new List<string>();
            var allExcludedProcesses = new List<string>();
            var allTitleContains = new List<string>();
            var allExcludedTitles = new List<string>();
            var allWindowStates = new List<WindowState>();

            foreach (var filter in filters)
            {
                if (filter.ProcessNames != null) allProcessNames.AddRange(filter.ProcessNames);
                if (filter.ExcludedProcesses != null) allExcludedProcesses.AddRange(filter.ExcludedProcesses);
                if (filter.TitleContains != null) allTitleContains.AddRange(filter.TitleContains);
                if (filter.ExcludedTitles != null) allExcludedTitles.AddRange(filter.ExcludedTitles);
                if (filter.WindowStates != null) allWindowStates.AddRange(filter.WindowStates);

                combined.MinWidth = Math.Max(combined.MinWidth ?? 0, filter.MinWidth ?? 0);
                combined.MinHeight = Math.Max(combined.MinHeight ?? 0, filter.MinHeight ?? 0);
            }

            if (allProcessNames.Any()) combined.ProcessNames = allProcessNames.Distinct().ToList();
            if (allExcludedProcesses.Any()) combined.ExcludedProcesses = allExcludedProcesses.Distinct().ToList();
            if (allTitleContains.Any()) combined.TitleContains = allTitleContains.Distinct().ToList();
            if (allExcludedTitles.Any()) combined.ExcludedTitles = allExcludedTitles.Distinct().ToList();
            if (allWindowStates.Any()) combined.WindowStates = allWindowStates.Distinct().ToList();

            return combined;
        }
    }
}
