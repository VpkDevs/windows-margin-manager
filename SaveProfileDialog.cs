using System;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class SaveProfileDialog : Form
    {
        public string ProfileName { get; private set; } = string.Empty;
        public string ProfileDescription { get; private set; } = string.Empty;

        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private Button okButton;
        private Button cancelButton;

        public SaveProfileDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Save Profile";
            this.Size = new System.Drawing.Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var nameLabel = new Label
            {
                Text = "Profile Name:",
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(100, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            nameTextBox = new TextBox
            {
                Location = new System.Drawing.Point(118, 12),
                Size = new System.Drawing.Size(250, 23),
                TabIndex = 0
            };

            var descriptionLabel = new Label
            {
                Text = "Description:",
                Location = new System.Drawing.Point(12, 50),
                Size = new System.Drawing.Size(100, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            descriptionTextBox = new TextBox
            {
                Location = new System.Drawing.Point(118, 47),
                Size = new System.Drawing.Size(250, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                TabIndex = 1
            };

            okButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(212, 125),
                Size = new System.Drawing.Size(75, 23),
                DialogResult = DialogResult.OK,
                TabIndex = 2
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(293, 125),
                Size = new System.Drawing.Size(75, 23),
                DialogResult = DialogResult.Cancel,
                TabIndex = 3
            };

            okButton.Click += OkButton_Click;
            nameTextBox.TextChanged += NameTextBox_TextChanged;

            this.Controls.AddRange(new Control[] 
            { 
                nameLabel, nameTextBox, descriptionLabel, descriptionTextBox, 
                okButton, cancelButton 
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            NameTextBox_TextChanged(null, EventArgs.Empty);
        }

        private void NameTextBox_TextChanged(object? sender, EventArgs e)
        {
            okButton.Enabled = !string.IsNullOrWhiteSpace(nameTextBox.Text);
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            ProfileName = nameTextBox.Text.Trim();
            ProfileDescription = descriptionTextBox.Text.Trim();
        }
    }
}
