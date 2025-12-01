using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WindowsMarginManager
{
    public class ProfileManager
    {
        private readonly string profilesPath;
        private List<Profile> profiles;

        public ProfileManager()
        {
            profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowsMarginManager",
                "profiles.json"
            );
            
            profiles = new List<Profile>();
            LoadProfiles();
            
            if (!profiles.Any())
            {
                CreateDefaultProfiles();
            }
        }

        public List<Profile> GetProfiles() => new List<Profile>(profiles);

        public Profile? GetProfile(string name)
        {
            return profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void SaveProfile(Profile profile)
        {
            var existing = profiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                profiles.Remove(existing);
            }
            
            profile.Id = existing?.Id ?? Guid.NewGuid().ToString();
            profile.LastModified = DateTime.UtcNow;
            profiles.Add(profile);
            
            SaveProfiles();
        }

        public void DeleteProfile(string name)
        {
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (profile != null)
            {
                profiles.Remove(profile);
                SaveProfiles();
            }
        }

        public void RenameProfile(string oldName, string newName)
        {
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
            if (profile != null)
            {
                profile.Name = newName;
                profile.LastModified = DateTime.UtcNow;
                SaveProfiles();
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(profilesPath))
                {
                    var json = File.ReadAllText(profilesPath);
                    var loadedProfiles = JsonConvert.DeserializeObject<List<Profile>>(json);
                    if (loadedProfiles != null)
                    {
                        profiles = loadedProfiles;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }

        private void SaveProfiles()
        {
            try
            {
                var directory = Path.GetDirectoryName(profilesPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
                File.WriteAllText(profilesPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }

        private void CreateDefaultProfiles()
        {
            var defaultProfiles = new[]
            {
                new Profile
                {
                    Name = "Work",
                    MarginSettings = new MarginSettings
                    {
                        LeftMargin = 100,
                        RightMargin = 300,
                        TopMargin = 50,
                        BottomMargin = 100,
                        UsePercentage = false,
                        EnableAnimations = true,
                        AnimationDuration = 400
                    },
                    WindowFilter = new WindowFilterCriteria
                    {
                        ExcludedProcesses = new List<string> { "explorer", "dwm", "winlogon" }
                    }
                },
                new Profile
                {
                    Name = "Gaming",
                    MarginSettings = new MarginSettings
                    {
                        LeftMargin = 2,
                        RightMargin = 2,
                        TopMargin = 2,
                        BottomMargin = 2,
                        UsePercentage = true,
                        EnableAnimations = false
                    },
                    WindowFilter = new WindowFilterCriteria
                    {
                        ProcessNames = new List<string> { "steam", "discord", "obs64" }
                    }
                },
                new Profile
                {
                    Name = "Design",
                    MarginSettings = new MarginSettings
                    {
                        LeftMargin = 5,
                        RightMargin = 5,
                        TopMargin = 3,
                        BottomMargin = 7,
                        UsePercentage = true,
                        EnableAnimations = true,
                        AnimationDuration = 600
                    },
                    WindowFilter = new WindowFilterCriteria
                    {
                        ProcessNames = new List<string> { "photoshop", "illustrator", "figma", "sketch" }
                    }
                },
                new Profile
                {
                    Name = "Minimal",
                    MarginSettings = new MarginSettings
                    {
                        LeftMargin = 20,
                        RightMargin = 20,
                        TopMargin = 20,
                        BottomMargin = 20,
                        UsePercentage = false,
                        EnableAnimations = true,
                        AnimationDuration = 200
                    }
                }
            };

            foreach (var profile in defaultProfiles)
            {
                SaveProfile(profile);
            }
        }
    }

    /// <summary>
    /// Represents a named profile containing margin settings and window filter criteria.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets or sets the unique identifier for this profile.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the display name of this profile.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the margin settings for this profile.
        /// </summary>
        public MarginSettings MarginSettings { get; set; } = new MarginSettings();

        /// <summary>
        /// Gets or sets the window filter criteria for this profile.
        /// </summary>
        public WindowFilterCriteria? WindowFilter { get; set; }

        /// <summary>
        /// Gets or sets when this profile was created.
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this profile was last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the description of this profile.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets tags for categorizing this profile.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this is the default profile.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Gets or sets custom properties for extensibility.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Returns a string representation of this profile.
        /// </summary>
        public override string ToString() => Name;
    }
}
