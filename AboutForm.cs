using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "About Windows Margin Manager Pro";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var iconPictureBox = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(64, 64),
                Image = SystemIcons.Application.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            var titleLabel = new Label
            {
                Text = "Windows Margin Manager Pro",
                Location = new Point(100, 20),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            var versionLabel = new Label
            {
                Text = $"Version {GetAssemblyVersion()}",
                Location = new Point(100, 50),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9)
            };

            var descriptionLabel = new Label
            {
                Text = "A comprehensive window management utility that provides intelligent margin-based window positioning with advanced features including multi-monitor support, animation effects, profile management, and smart window filtering.",
                Location = new Point(20, 100),
                Size = new Size(400, 60),
                Font = new Font("Segoe UI", 9)
            };

            var featuresLabel = new Label
            {
                Text = "Key Features:",
                Location = new Point(20, 170),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var featuresList = new Label
            {
                Text = "• Global hotkey support (Ctrl+Alt+M)\n" +
                       "• Multi-monitor awareness and optimization\n" +
                       "• Smooth animation effects with multiple easing types\n" +
                       "• Profile management with quick switching (Ctrl+Alt+1-9)\n" +
                       "• Advanced window filtering and targeting\n" +
                       "• Customizable margins (pixels or percentage)\n" +
                       "• System tray integration with context menu\n" +
                       "• Persistent settings and profile storage",
                Location = new Point(40, 195),
                Size = new Size(380, 120),
                Font = new Font("Segoe UI", 8)
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(350, 280),
                Size = new Size(75, 26),
                DialogResult = DialogResult.OK
            };

            this.Controls.AddRange(new Control[]
            {
                iconPictureBox, titleLabel, versionLabel, descriptionLabel,
                featuresLabel, featuresList, okButton
            });

            this.AcceptButton = okButton;
        }

        private string GetAssemblyVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }
    }
}
