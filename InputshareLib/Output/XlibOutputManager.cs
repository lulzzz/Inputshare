using System;
using InputshareLib.Input;
using static InputshareLib.Native.Linux.LibX11;
using static InputshareLib.Native.Linux.LibXtst;

namespace InputshareLib.Output
{
    public class XlibOutputManager : IOutputManager
    {
        public bool Running { get; private set; }

        private IntPtr xDisplay;
        private IntPtr xRootWindow;

        public void ResetKeyStates()
        {
            //TODO
        }

        public void Send(ISInputData input)
        {
            if (input.Code == ISInputCode.IS_MOUSEMOVERELATIVE)
            {
                XWarpPointer(xDisplay, IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, input.Param1, input.Param2);
            }
            else if (input.Code == ISInputCode.IS_MOUSELDOWN)
            {
                XTestFakeButtonEvent(xDisplay, X11_LEFTBUTTON, true, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSELUP)
            {
                XTestFakeButtonEvent(xDisplay, X11_LEFTBUTTON, false, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSERDOWN)
            {
                XTestFakeButtonEvent(xDisplay, X11_RIGHTBUTTON, true, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSERUP)
            {
                XTestFakeButtonEvent(xDisplay, X11_RIGHTBUTTON, false, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSEMDOWN)
            {
                XTestFakeButtonEvent(xDisplay, X11_MIDDLEBUTTON, true, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSEMUP)
            {
                XTestFakeButtonEvent(xDisplay, X11_MIDDLEBUTTON, false, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSEYSCROLL)
            {
                //Param1 contains the mouse direction, 120 = up; -120 = down
                if (input.Param1 < 0)
                {
                    XTestFakeButtonEvent(xDisplay, X11_SCROLLDOWN, true, 0);
                    XTestFakeButtonEvent(xDisplay, X11_SCROLLDOWN, false, 0);
                }
                else
                {
                    XTestFakeButtonEvent(xDisplay, X11_SCROLLUP, true, 0);
                    XTestFakeButtonEvent(xDisplay, X11_SCROLLUP, false, 0);
                }
            }
            else if (input.Code == ISInputCode.IS_MOUSEXSCROLL)
            {
                //todo
            }
            else if (input.Code == ISInputCode.IS_MOUSEXDOWN)
            {
                //first param is the ID of the button. 4 = forward, 5 = back
                if (input.Param1 == 4)
                    XTestFakeButtonEvent(xDisplay, X11_XBUTTONFORWARD, true, 0);
                else
                    XTestFakeButtonEvent(xDisplay, X11_XBUTTONBACK, true, 0);
            }
            else if (input.Code == ISInputCode.IS_MOUSEXUP)
            {
                if (input.Param1 == 4)
                    XTestFakeButtonEvent(xDisplay, X11_XBUTTONFORWARD, false, 0);
                else
                    XTestFakeButtonEvent(xDisplay, X11_XBUTTONBACK, false, 0);
            }
            else if (input.Code == ISInputCode.IS_KEYDOWN)
            {
                try
                {
                    XlibKeySym key = KeyMap.WinToXlib((WindowsVirtualKey)input.Param1);
                    uint k = XKeysymToKeycode(xDisplay, (int)key);
                    XTestFakeKeyEvent(xDisplay, k, true, 0);
                }catch(Exception ex)
                {
                    ISLogger.Write("XlibOutputManager: Error sending key: " + ex.Message);
                }
               
            }
            else if (input.Code == ISInputCode.IS_KEYUP)
            {
                try
                {
                    XlibKeySym key = KeyMap.WinToXlib((WindowsVirtualKey)input.Param1);
                    uint k = XKeysymToKeycode(xDisplay, (int)key);
                    XTestFakeKeyEvent(xDisplay, k, false, 0);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("XlibOutputManager: Error sending key: " + ex.Message);
                }

            }

            XFlush(xDisplay);
        }

        public void Start()
        {
            if (Running)
                throw new InvalidOperationException("XlibOutputManager already running");

            xDisplay = XOpenDisplay(0);
            Running = true;
        }

        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("XlibOutputManager not running");

            XCloseDisplay(xDisplay);
            Running = false;
        }
    }
}
