using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.Native
{
    public static class WindowMessages
    {
        public const int WM_CLOSE = 0x0010;

        public const int WM_DROPFILES = 563;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LMOUSEDOWN = 0x0201;
        public const int WM_LMOUSEUP = 0x0202;
        public const int WM_RMOUSEDOWN = 0x0204;
        public const int WM_RMOUSEUP = 0x0205;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
    }
}
