using System;
using System.Runtime.InteropServices;

namespace InputshareLibWindows.Native
{
    public static class Gdi32
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
