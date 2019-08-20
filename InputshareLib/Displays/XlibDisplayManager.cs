using System;
using System.Collections.Generic;
using static InputshareLib.Native.Linux.LibX11;

namespace InputshareLib.Displays
{
    public class XlibDisplayManager : DisplayManagerBase
    {
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
            IntPtr display = XOpenDisplay(0);
            
            //Get information about the root window to determine virtual display size.
            XGetWindowAttributes(display, XDefaultRootWindow(display), out XWindowAttributes window);
            XCloseDisplay(display);

            List<Display> displays = new List<Display>();

            //Todo - get individual displays
            displays.Add(new Display(new System.Drawing.Rectangle(window.x, window.y, window.width, window.height), 0, "ROOT", true));
            CurrentConfig = new DisplayConfig(new System.Drawing.Rectangle(window.x, window.y, window.width, window.height), new System.Collections.Generic.List<Display>()); ;
        }
    }
}
