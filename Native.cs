using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCopy {
    internal static class Native {
        [DllImport("user32.dll")] public static extern bool AddClipboardFormatListener(IntPtr h);
        [DllImport("user32.dll")] public static extern bool RemoveClipboardFormatListener(IntPtr h);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern uint SendInput(uint count, INPUT[] inputs, int size);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public UNION data; }
        [StructLayout(LayoutKind.Explicit)] public struct UNION { [FieldOffset(0)] public KEYBOARD keyboard; [FieldOffset(0)] public MOUSE mouse; }
        [StructLayout(LayoutKind.Sequential)] public struct KEYBOARD { public ushort key, scan; public uint flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MOUSE { public int x, y; public uint data, flags, time; public UIntPtr extra; }
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h, System.Text.StringBuilder name, int count);
        public static bool IsPasteTarget(IntPtr h) {
            if(h==IntPtr.Zero || !IsWindow(h) || ProcessName(h)==Process.GetCurrentProcess().ProcessName)return false;
            var name=new System.Text.StringBuilder(256); GetClassName(h,name,name.Capacity);
            string value=name.ToString();
            return value!="Shell_TrayWnd" && value!="Shell_SecondaryTrayWnd" && value!="NotifyIconOverflowWindow" && value!="TopLevelWindowForOverflowXamlIsland";
        }
        public static bool Paste() {
            INPUT[] keys = new INPUT[4];
            ushort[] codes = { 0x11, 0x56, 0x56, 0x11 };
            for (int i = 0; i < 4; i++) { keys[i].type = 1; keys[i].data.keyboard.key = codes[i]; keys[i].data.keyboard.flags = i >= 2 ? 2u : 0u; }
            return SendInput(4, keys, Marshal.SizeOf(typeof(INPUT))) == 4;
        }
        public static string ProcessName(IntPtr window) {
            try { uint pid; GetWindowThreadProcessId(window, out pid); return Process.GetProcessById((int)pid).ProcessName; } catch { return ""; }
        }
        public static bool ModifiersDown() { return new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }.AnyDown(); }
        static bool AnyDown(this int[] keys) { foreach (int key in keys) if ((GetAsyncKeyState(key) & 0x8000) != 0) return true; return false; }
    }
}
