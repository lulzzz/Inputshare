using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using InputshareLib;
using InputshareLib.Displays;
using InputshareLibWindows.Native;
using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows.Displays
{
    public class WindowsDisplayManager : DisplayManagerBase
    {
        private const int CX_VIRTUALSCREEN = 78;
        private const int CY_VIRTUALSCREEN = 79;

        public WindowsDisplayManager()
        {
            UpdateConfigManual();
        }
        
        private Display GetDisplay(int index)
        {
            if(Screen.AllScreens[index] == null)
                throw new ArgumentException("Display does not exist");

            Display indexDisplay = null;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref W32Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.Size = Marshal.SizeOf(mi);
                    int displayIndex = 1;
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        if (displayIndex == index)
                        {
                            Rectangle r = new Rectangle(mi.Monitor.left, mi.Monitor.bottom,
                         Math.Abs(mi.Monitor.right - mi.Monitor.left), Math.Abs(mi.Monitor.top - mi.Monitor.bottom));
                            indexDisplay = new Display(r, index, mi.DeviceName, mi.Flags > 0);
                            return false;
                        }

                        displayIndex++;
                    }
                    return true;
                }, IntPtr.Zero);

            if (indexDisplay == null)
                throw new Exception("Could not find monitor at index " + index);

            return indexDisplay;
        }

        private DisplayConfig GetDisplayConfig()
        {
            //
            int w = GetSystemMetrics(CX_VIRTUALSCREEN);
            int h = GetSystemMetrics(CY_VIRTUALSCREEN);
            Rectangle vBounds = new Rectangle(0, 0,0,0);

            List<Display> displays = new List<Display>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, 
                delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref W32Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFOEX mi = new MONITORINFOEX();
                    mi.Size = Marshal.SizeOf(mi);
                    int displayIndex = 1;
                    if(GetMonitorInfo(hMonitor, ref mi))
                    {
                        Rectangle r = Rectangle.FromLTRB(mi.Monitor.left, mi.Monitor.top, mi.Monitor.right, mi.Monitor.bottom);
                        vBounds = Rectangle.Union(vBounds, r);
                        displays.Add(new Display(r, displayIndex, mi.DeviceName, (mi.Flags != 0)));
                        displayIndex++;
                    }
                    return true;
                }, IntPtr.Zero);

            return new DisplayConfig(vBounds, displays);
        }

        public override void StartMonitoring()
        {
            Running = true;
        }

        public override void StopMonitoring()
        {
            Running = false;
        }

        public override void UpdateConfigManual()
        {
            CurrentConfig = GetDisplayConfig();
        }
    }
}
