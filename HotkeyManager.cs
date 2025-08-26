using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsMarginManager
{
    public class HotkeyManager
    {
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
        
        private const int WM_HOTKEY = 0x0312;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly Form parentForm;
        private readonly Dictionary<int, Action> hotkeyActions;
        private int nextHotkeyId = 1;

        public HotkeyManager(Form parentForm)
        {
            this.parentForm = parentForm;
            this.hotkeyActions = new Dictionary<int, Action>();
            
            var originalWndProc = parentForm.GetType().GetMethod("WndProc", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            parentForm.GetType().GetMethod("WndProc", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        public int RegisterHotkey(Keys key, int modifiers, Action action)
        {
            int hotkeyId = nextHotkeyId++;
            
            if (RegisterHotKey(parentForm.Handle, hotkeyId, modifiers, (int)key))
            {
                hotkeyActions[hotkeyId] = action;
                
                Application.AddMessageFilter(new HotkeyMessageFilter(hotkeyActions));
                
                return hotkeyId;
            }
            
            throw new InvalidOperationException($"Failed to register hotkey: {key} with modifiers {modifiers}");
        }

        public void UnregisterHotkey(int hotkeyId)
        {
            if (hotkeyActions.ContainsKey(hotkeyId))
            {
                UnregisterHotKey(parentForm.Handle, hotkeyId);
                hotkeyActions.Remove(hotkeyId);
            }
        }

        public void UnregisterHotkeys()
        {
            foreach (var hotkeyId in hotkeyActions.Keys)
            {
                UnregisterHotKey(parentForm.Handle, hotkeyId);
            }
            hotkeyActions.Clear();
        }
    }

    public class HotkeyMessageFilter : IMessageFilter
    {
        private const int WM_HOTKEY = 0x0312;
        private readonly Dictionary<int, Action> hotkeyActions;

        public HotkeyMessageFilter(Dictionary<int, Action> hotkeyActions)
        {
            this.hotkeyActions = hotkeyActions;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyActions.TryGetValue(hotkeyId, out Action? action))
                {
                    action?.Invoke();
                    return true;
                }
            }
            return false;
        }
    }
}
