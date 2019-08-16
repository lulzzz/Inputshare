using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace InputshareLibWindows.Native
{
    public static class Gdi32
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
