using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class ProfileManagerForm : Form
    {
        private readonly ProfileManager profileManager;
        private ListBox profileListBox;
        private TextBox nameTextBox, descriptionTextBox;
        private Button newButton, editButton, deleteButton, duplicateButton;
        private Button okButton;
        private GroupBox detailsGroupBox;
        private Label marginsLabel, filtersLabel, createdLabel, modifiedLabel;

        public ProfileManagerForm(ProfileManager profileManager)
        {
            this.profileManager = profileManager;
            InitializeComponent();
            LoadProfiles();
        }

        private void InitializeComponent()
        {
            this.Text = "Profile Manager";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(500, 400);

            CreateProfileList();
            CreateDetailsPanel();
            CreateButtonPanel();
        }

        private void CreateProfileList()
        {
            var listLabel = new Label
            {
                Text = "Profiles:",
                Location = new Point(12, 12),
                Size = new Size(100, 23)
            };

            profileListBox = new ListBox
            {
                Location = new Point(12, 38),
                Size = new Size(200, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            profileListBox.SelectedIndexChanged += ProfileListBox_SelectedIndexChanged;

            var listButtonPanel = new Panel
            {
                Location = new Point(12, 344),
                Size = new Size(200, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            newButton = new Button
            {
                Text = "New",
                Location = new Point(0, 0),
                Size = new Size(45, 26)
            };

            editButton = new Button
            {
                Text = "Edit",
                Location = new Point(50, 0),
                Size = new Size(45, 26),
                Enabled = false
            };

            deleteButton = new Button
            {
                Text = "Delete",
                Location = new Point(100, 0),
                Size = new Size(50, 26),
                Enabled = false
            };

            duplicateButton = new Button
            {
                Text = "Copy",
                Location = new Point(155, 0),
                Size = new Size(45, 26),
                Enabled = false
            };

            newButton.Click += NewButton_Click;
            editButton.Click += EditButton_Click;
            deleteButton.Click += DeleteButton_Click;
            duplicateButton.Click += DuplicateButton_Click;

            listButtonPanel.Controls.AddRange(new Control[] { newButton, editButton, deleteButton, duplicateButton });

            this.Controls.AddRange(new Control[] { listLabel, profileListBox, listButtonPanel });
        }

        private void CreateDetailsPanel()
        {
            detailsGroupBox = new GroupBox
            {
                Text = "Profile Details",
                Location = new Point(225, 12),
                Size = new Size(350, 326),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(12, 25),
                Size = new Size(50, 23)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(68, 22),
                Size = new Size(270, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true
            };

            var descLabel = new Label
            {
                Text = "Description:",
                Location = new Point(12, 55),
                Size = new Size(80, 23)
            };

            descriptionTextBox = new TextBox
            {
                Location = new Point(12, 78),
                Size = new Size(326, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            marginsLabel = new Label
            {
                Text = "Margins: Not selected",
                Location = new Point(12, 150),
                Size = new Size(326, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            filtersLabel = new Label
            {
                Text = "Filters: Not selected",
                Location = new Point(12, 195),
                Size = new Size(326, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            createdLabel = new Label
            {
                Text = "Created: Not selected",
                Location = new Point(12, 240),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            modifiedLabel = new Label
            {
                Text = "Modified: Not selected",
                Location = new Point(12, 265),
                Size = new Size(326, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            detailsGroupBox.Controls.AddRange(new Control[]
            {
                nameLabel, nameTextBox, descLabel, descriptionTextBox,
                marginsLabel, filtersLabel, createdLabel, modifiedLabel
            });

            this.Controls.Add(detailsGroupBox);
        }

        private void CreateButtonPanel()
        {
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            okButton = new Button
            {
                Text = "Close",
                Location = new Point(500, 12),
                Size = new Size(75, 26),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            buttonPanel.Controls.Add(okButton);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = okButton;
        }

        private void LoadProfiles()
        {
            profileListBox.Items.Clear();
            var profiles = profileManager.GetProfiles();
            foreach (var profile in profiles)
            {
                profileListBox.Items.Add(profile);
            }
            profileListBox.DisplayMember = "Name";
        }

        private void ProfileListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = profileListBox.SelectedItem != null;
            editButton.Enabled = hasSelection;
            deleteButton.Enabled = hasSelection;
            duplicateButton.Enabled = hasSelection;

            if (profileListBox.SelectedItem is Profile profile)
            {
                ShowProfileDetails(profile);
            }
            else
            {
                ClearProfileDetails();
            }
        }

        private void ShowProfileDetails(Profile profile)
        {
            nameTextBox.Text = profile.Name;
            descriptionTextBox.Text = profile.Description;

            var margins = profile.MarginSettings;
            marginsLabel.Text = $"Margins: L:{margins.LeftMargin} T:{margins.TopMargin} R:{margins.RightMargin} B:{margins.BottomMargin}" +
                               $" ({(margins.UsePercentage ? "%" : "px")})";

            var filterCount = (profile.WindowFilter?.ProcessNames?.Count ?? 0) + 
                             (profile.WindowFilter?.ExcludedProcesses?.Count ?? 0);
            filtersLabel.Text = $"Filters: {filterCount} rules configured";

            createdLabel.Text = $"Created: {profile.Created:yyyy-MM-dd HH:mm}";
            modifiedLabel.Text = $"Modified: {profile.LastModified:yyyy-MM-dd HH:mm}";
        }

        private void ClearProfileDetails()
        {
            nameTextBox.Text = "";
            descriptionTextBox.Text = "";
            marginsLabel.Text = "Margins: Not selected";
            filtersLabel.Text = "Filters: Not selected";
            createdLabel.Text = "Created: Not selected";
            modifiedLabel.Text = "Modified: Not selected";
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            var dialog = new SaveProfileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var profile = new Profile
                {
                    Name = dialog.ProfileName,
                    Description = dialog.ProfileDescription,
                    MarginSettings = new MarginSettings(),
                    WindowFilter = new WindowFilterCriteria()
                };

                profileManager.SaveProfile(profile);
                LoadProfiles();

                var index = profileListBox.Items.Cast<Profile>().ToList().FindIndex(p => p.Name == profile.Name);
                if (index >= 0)
                {
                    profileListBox.SelectedIndex = index;
                }
            }
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (profileListBox.SelectedItem is Profile profile)
            {
                var settingsForm = new AdvancedSettingsForm(profile.MarginSettings, 
                    profile.WindowFilter ?? new WindowFilterCriteria(), profileManager);
                
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    profile.MarginSettings = settingsForm.MarginSettings;
                    profile.WindowFilter = settingsForm.WindowFilter;
                    profileManager.SaveProfile(profile);
                    ShowProfileDetails(profile);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (profileListBox.SelectedItem is Profile profile)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the profile '{profile.Name}'?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    profileManager.DeleteProfile(profile.Name);
                    LoadProfiles();
                    ClearProfileDetails();
                }
            }
        }

        private void DuplicateButton_Click(object? sender, EventArgs e)
        {
            if (profileListBox.SelectedItem is Profile originalProfile)
            {
                var dialog = new SaveProfileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var duplicatedProfile = new Profile
                    {
                        Name = dialog.ProfileName,
                        Description = dialog.ProfileDescription,
                        MarginSettings = new MarginSettings(originalProfile.MarginSettings),
                        WindowFilter = new WindowFilterCriteria
                        {
                            ProcessNames = originalProfile.WindowFilter?.ProcessNames?.ToList(),
                            ExcludedProcesses = originalProfile.WindowFilter?.ExcludedProcesses?.ToList(),
                            PrimaryMonitorOnly = originalProfile.WindowFilter?.PrimaryMonitorOnly ?? false
                        }
                    };

                    profileManager.SaveProfile(duplicatedProfile);
                    LoadProfiles();

                    var index = profileListBox.Items.Cast<Profile>().ToList().FindIndex(p => p.Name == duplicatedProfile.Name);
                    if (index >= 0)
                    {
                        profileListBox.SelectedIndex = index;
                    }
                }
            }
        }
    }
}
