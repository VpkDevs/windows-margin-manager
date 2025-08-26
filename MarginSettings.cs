using System;
using System.IO;
using System.Text.Json;

namespace WindowsMarginManager
{
    public class MarginSettings
    {
        public int LeftMargin { get; set; } = 50;
        public int TopMargin { get; set; } = 50;
        public int RightMargin { get; set; } = 50;
        public int BottomMargin { get; set; } = 50;
        public bool UsePercentage { get; set; } = false;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsMarginManager",
            "settings.json"
        );

        public MarginSettings()
        {
            LoadSettings();
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

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
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
                    var settings = JsonSerializer.Deserialize<MarginSettings>(json);
                    
                    if (settings != null)
                    {
                        LeftMargin = settings.LeftMargin;
                        TopMargin = settings.TopMargin;
                        RightMargin = settings.RightMargin;
                        BottomMargin = settings.BottomMargin;
                        UsePercentage = settings.UsePercentage;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to load settings, using defaults: {ex.Message}", 
                    "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }
}
