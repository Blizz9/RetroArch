using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ParasiteDriver
{
    public class HotKey
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x00;
        private const uint MOD_ALT = 0x01;
        private const uint MOD_CONTROL = 0x02;
        private const uint MOD_SHIFT = 0x04;
        private const uint MOD_WIN = 0x08;
        private const uint VK_CAPITAL = 0x14;
        private const uint VK_OEM_PERIOD = 0xBE;

        private IntPtr _windowHandle;
        private HwndSource _windowHandleSource;

        public event Action Pressed;

        public HotKey(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
            _windowHandleSource = HwndSource.FromHwnd(_windowHandle);
            _windowHandleSource.AddHook(hotKeyHookHandler);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_OEM_PERIOD);
        }

        public void Cleanup()
        {
            _windowHandleSource.RemoveHook(hotKeyHookHandler);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        private IntPtr hotKeyHookHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    int vkey = (((int)lParam >> 16) & 0xFFFF);
                    if (vkey == VK_OEM_PERIOD)
                    {
                        Pressed?.Invoke();
                    }

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }
    }
}
