using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace sbx
{
    /// <summary>
    /// VirtualKey is the key code used to read keyboard events.
    /// </summary>
    enum VirtualKey
    {
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1,
        VK_NUMPAD2,
        VK_NUMPAD3,
        VK_NUMPAD4,
        VK_NUMPAD5,
        VK_NUMPAD6,
        VK_NUMPAD7,
        VK_NUMPAD8,
        VK_NUMPAD9,
        VK_MULTIPLY,
        VK_ADD,
        VK_SEPARATOR,
        VK_SUBTRACT,
        VK_DECIMAL,
        VK_DIVIDE,
    };

    /// <summary>
    /// HotKey is the internal hotkey ID.
    /// </summary>
    enum HotKey
    {
        FIRST,
        KP_1 = FIRST,
        KP_2,
        KP_3,
        KP_4,
        KP_5,
        KP_6,
        KP_7,
        KP_8,
        KP_9,
        KP_PLUS,
        KP_MINUS,
        KP_MULTIPLY,
        KP_DIVIDE,
        LAST,
    };

    internal class Hotkeys
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        IntPtr hwnd;

        Dictionary<HotKey, Action<HotKey>> actions = new();

        public Hotkeys(Window wnd)
        {
            hwnd = new WindowInteropHelper(wnd).Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            RegisterHotKey(hwnd, (int)HotKey.KP_1, 0, (int)VirtualKey.VK_NUMPAD1);
            RegisterHotKey(hwnd, (int)HotKey.KP_2, 0, (int)VirtualKey.VK_NUMPAD2);
            RegisterHotKey(hwnd, (int)HotKey.KP_3, 0, (int)VirtualKey.VK_NUMPAD3);
            RegisterHotKey(hwnd, (int)HotKey.KP_4, 0, (int)VirtualKey.VK_NUMPAD4);
            RegisterHotKey(hwnd, (int)HotKey.KP_5, 0, (int)VirtualKey.VK_NUMPAD5);
            RegisterHotKey(hwnd, (int)HotKey.KP_6, 0, (int)VirtualKey.VK_NUMPAD6);
            RegisterHotKey(hwnd, (int)HotKey.KP_7, 0, (int)VirtualKey.VK_NUMPAD7);
            RegisterHotKey(hwnd, (int)HotKey.KP_8, 0, (int)VirtualKey.VK_NUMPAD8);
            RegisterHotKey(hwnd, (int)HotKey.KP_9, 0, (int)VirtualKey.VK_NUMPAD9);
            RegisterHotKey(hwnd, (int)HotKey.KP_PLUS, 0, (int)VirtualKey.VK_ADD);
            RegisterHotKey(hwnd, (int)HotKey.KP_MINUS, 0, (int)VirtualKey.VK_SUBTRACT);
            RegisterHotKey(hwnd, (int)HotKey.KP_MULTIPLY, 0, (int)VirtualKey.VK_MULTIPLY);
            RegisterHotKey(hwnd, (int)HotKey.KP_DIVIDE, 0, (int)VirtualKey.VK_DIVIDE);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_HOTKEY:
                    var hk = (HotKey)wParam.ToInt32();
                    actions[hk](hk);
                    break;
            }
            return IntPtr.Zero;
        }

        internal void Register(HotKey hotkey, Action<HotKey> action)
        {
            actions[hotkey] = action;
        }

        internal void Register(HotKey hotkey, Action action)
        {
            actions[hotkey] = (k) => action();
        }
    }
}
