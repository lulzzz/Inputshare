using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace InputshareLib.Native.Linux
{
    public static class LibXtst
    {
        public const int X11_LEFTBUTTON = 1;
        public const int X11_MIDDLEBUTTON = 2;
        public const int X11_RIGHTBUTTON = 3;
        public const int X11_SCROLLUP = 4;
        public const int X11_SCROLLDOWN = 5;
        public const int X11_SCROSSLEFT = 6;
        public const int X11_SCROLLRIGHT = 7;
        public const int X11_XBUTTONBACK = 8;
        public const int X11_XBUTTONFORWARD = 9;

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool pressed, int delay);

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeMotionEvent(IntPtr display, int screenNumber, int x, int y, uint delay);

        [DllImport("libXtst.so.6")]
        public static extern int XTestFakeButtonEvent(IntPtr display, int button, bool pressed, int delay);
    }
}
