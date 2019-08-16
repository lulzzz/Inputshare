using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;

namespace InputshareLibWindows.Native
{
    public static class User32
    {

        [DllImport("USER32.dll")]
        public static extern short GetKeyState(System.Windows.Forms.Keys nVirtKey);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        public static extern short VkKeyScanA(char ch);

        public enum MAPVKTYPE
        {
            MAPVK_VK_TO_CHAR = 2,
            MAPVK_VK_TO_VSC = 0,
            MAPVK_VSC_TO_VK = 1,
            MAPVK_VSC_TO_KEY_EX
        }

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyA(uint code, MAPVKTYPE type);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref W32Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        private const int CCHDEVICENAME = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int Size;
            public W32Rect Monitor;
            public W32Rect WorkArea;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName;

            public void Init()
            {
                this.Size = 40 + 2 * CCHDEVICENAME;
                this.DeviceName = string.Empty;
            }
        }

        [DllImport("user32.dll")]
        public static extern bool RemoveClipboardFormatListener(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int metric);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT pos);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelHookCallback lpfn, IntPtr hMod, uint dwThreadId);
        public delegate IntPtr LowLevelHookCallback(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
          uint idThread, uint dwFlags);
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            IntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt16 RegisterClassEx(ref WNDCLASSEX classEx);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(HandleRef hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessage(out MSG message, IntPtr hwnd, uint min, uint max);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int code);

       

        [DllImport("user32.dll")]
        public static extern bool UnregisterClassA(string className, IntPtr hInstance);

        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public int time;
            public POINT pt;
        }

        public struct POINT
        {
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern void DispatchMessage(ref MSG message);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr wnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(int dwExStyle,
              [MarshalAs(UnmanagedType.LPStr)]
               string lpClassName,
              [MarshalAs(UnmanagedType.LPStr)]
               string lpWindowName,
              UInt32 dwStyle,
              int x,
              int y,
              int nWidth,
              int nHeight,
              IntPtr hWndParent,
              IntPtr hMenu,
              IntPtr hInstance,
              IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProcA(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool GetClassInfoExA(IntPtr hInstance, string lpClassName, ref WNDCLASSEX lpWndClass);

        public struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);

        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;

        [StructLayout(LayoutKind.Sequential)]
        public struct W32Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

    }
}
