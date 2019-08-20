using System;
using System.Drawing;
using System.Threading;
using static InputshareLib.Native.Linux.LibX11;

namespace InputshareLib.Cursor
{
    public class XlibCursorMonitor : CursorMonitorBase
    {
        private IntPtr xDisplay;
        private IntPtr xRootWindow;

        private Timer cursorUpdateTimer;
        private Rectangle screenBounds;

        public override void StartMonitoring(Rectangle bounds)
        {
            if (Running)
                throw new InvalidOperationException("XlibCursorMonitor already running");

            xDisplay = XOpenDisplay(0);
            xRootWindow = XDefaultRootWindow(xDisplay);
            screenBounds = bounds;
            //Update 20 times per second
            cursorUpdateTimer = new Timer(CursorUpdateTimerCallback, null, 0, 50);
            Running = true;
        }

        private void CursorUpdateTimerCallback(object sync)
        {
            //We just need the cursor position relative to the root window, which is the full screen.
            XQueryPointer(xDisplay, xRootWindow, out _, out _, out int posX, out int posY, out _, out _, out _);

            //see if the cursor has hit an edge
            if (posY == screenBounds.Bottom - 1)
                HandleEdgeHit(Edge.Bottom);
            if (posY == screenBounds.Top)
                HandleEdgeHit(Edge.Top);
            if (posX == screenBounds.Left)
                HandleEdgeHit(Edge.Left);
            if (posX == screenBounds.Right - 1)
                HandleEdgeHit(Edge.Right);
        }

        public override void StopMonitoring()
        {
            if (!Running)
                throw new InvalidOperationException("XlibCursorMonitor not running");

            XCloseDisplay(xDisplay);
            cursorUpdateTimer?.Dispose();
            Running = false;
        }
    }
}
