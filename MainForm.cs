using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private HotkeyManager hotkeyManager;
        private WindowManager windowManager;
        private MarginSettings marginSettings;
        private List<WindowInfo> originalWindowStates;
        private bool marginsApplied = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            
            marginSettings = new MarginSettings();
            hotkeyManager = new HotkeyManager(this);
            windowManager = new WindowManager();
            originalWindowStates = new List<WindowInfo>();
            
            RegisterHotkey();
            
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        private void InitializeComponent()
        {
            this.Text = "Windows Margin Manager";
            this.Size = new Size(1, 1);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            
            var settingsItem = new ToolStripMenuItem("Settings", null, OnSettings);
            var toggleItem = new ToolStripMenuItem("Toggle Margins (Ctrl+Alt+M)", null, OnToggleMargins);
            var exitItem = new ToolStripMenuItem("Exit", null, OnExit);
            
            trayMenu.Items.Add(toggleItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(settingsItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "Windows Margin Manager"
            };
            
            trayIcon.DoubleClick += OnToggleMargins;
        }

        private void RegisterHotkey()
        {
            hotkeyManager.RegisterHotkey(Keys.M, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT, OnHotkeyPressed);
        }

        private void OnHotkeyPressed()
        {
            ToggleMargins();
        }

        private void OnToggleMargins(object? sender, EventArgs e)
        {
            ToggleMargins();
        }

        private void ToggleMargins()
        {
            if (marginsApplied)
            {
                RestoreWindows();
            }
            else
            {
                ApplyMargins();
            }
        }

        private void ApplyMargins()
        {
            try
            {
                originalWindowStates.Clear();
                var windows = windowManager.GetTopLevelWindows();
                
                var screen = Screen.PrimaryScreen;
                var workingArea = screen.WorkingArea;
                
                int marginLeft, marginTop, marginRight, marginBottom;
                
                if (marginSettings.UsePercentage)
                {
                    marginLeft = (int)(workingArea.Width * marginSettings.LeftMargin / 100);
                    marginTop = (int)(workingArea.Height * marginSettings.TopMargin / 100);
                    marginRight = (int)(workingArea.Width * marginSettings.RightMargin / 100);
                    marginBottom = (int)(workingArea.Height * marginSettings.BottomMargin / 100);
                }
                else
                {
                    marginLeft = marginSettings.LeftMargin;
                    marginTop = marginSettings.TopMargin;
                    marginRight = marginSettings.RightMargin;
                    marginBottom = marginSettings.BottomMargin;
                }

                var targetRect = new Rectangle(
                    workingArea.Left + marginLeft,
                    workingArea.Top + marginTop,
                    workingArea.Width - marginLeft - marginRight,
                    workingArea.Height - marginTop - marginBottom
                );

                foreach (var window in windows)
                {
                    originalWindowStates.Add(new WindowInfo
                    {
                        Handle = window.Handle,
                        Rectangle = window.Rectangle,
                        IsMaximized = window.IsMaximized
                    });
                    
                    windowManager.SetWindowPosition(window.Handle, targetRect);
                }
                
                marginsApplied = true;
                trayIcon.Text = "Windows Margin Manager - Margins Applied";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying margins: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreWindows()
        {
            try
            {
                foreach (var windowInfo in originalWindowStates)
                {
                    if (windowInfo.IsMaximized)
                    {
                        windowManager.MaximizeWindow(windowInfo.Handle);
                    }
                    else
                    {
                        windowManager.SetWindowPosition(windowInfo.Handle, windowInfo.Rectangle);
                    }
                }
                
                originalWindowStates.Clear();
                marginsApplied = false;
                trayIcon.Text = "Windows Margin Manager";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restoring windows: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(marginSettings);
            settingsForm.ShowDialog();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            hotkeyManager.UnregisterHotkeys();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hotkeyManager?.UnregisterHotkeys();
                trayIcon?.Dispose();
                trayMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public Rectangle Rectangle { get; set; }
        public bool IsMaximized { get; set; }
    }
}
