﻿using InputshareLib;
using InputshareLib.Input;
using InputshareLib.Output;
using System;
using System.Runtime.InteropServices;
using static InputshareLibWindows.Native.User32;

namespace InputshareLibWindows.Output
{
    public class WindowsOutputManager : IOutputManager
    {
        public bool Running { get; private set; }

        public void Send(ISInputData input)
        {
            ISInputCode c = input.Code;
            switch (c)
            {
                case ISInputCode.IS_MOUSEMOVERELATIVE:
                    MoveMouseRelative(input.Param1, input.Param2);
                    break;
                case ISInputCode.IS_KEYDOWN:
                    {
                        short useKey = CheckKey(input, out bool useScan);

                        if (useScan)
                            KeyDownScan(useKey, true);
                        else
                            KeyDownVirtual(useKey, true);

                        break;
                    }
                case ISInputCode.IS_KEYUP:
                    {
                        short useKey = CheckKey(input, out bool useScan);

                        if (useScan)
                            KeyDownScan(useKey, false);
                        else
                            KeyDownVirtual(useKey, false);

                        break;
                    }
                case ISInputCode.IS_MOUSELDOWN:
                    MouseLDown(true);
                    break;
                case ISInputCode.IS_MOUSELUP:
                    MouseLDown(false);
                    break;
                case ISInputCode.IS_MOUSERDOWN:
                    MouseRDown(true);
                    break;
                case ISInputCode.IS_MOUSERUP:
                    MouseRDown(false);
                    break;
                case ISInputCode.IS_MOUSEMDOWN:
                    MouseMDown(true);
                    break;
                case ISInputCode.IS_MOUSEMUP:
                    MouseMDown(false);
                    break;
                case ISInputCode.IS_MOUSEYSCROLL:
                    MouseYScroll(input.Param1);
                    break;
                case ISInputCode.IS_MOUSEXDOWN:
                    MouseXDown(input.Param1, true);
                    break;
                case ISInputCode.IS_MOUSEXUP:
                    MouseXDown(input.Param1, false);
                    break;
                case ISInputCode.IS_RELEASEALL:
                    ResetKeyStates();
                    break;
                case ISInputCode.IS_MOUSEMOVEABSOLUTE:
                    MoveMouseAbs(input.Param1, input.Param2);
                    break;
            }
        }
        private void MoveMouseAbs(short x, short y)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse

            POINT pt = ConvertScreenPointToAbsolutePoint((uint)x, (uint)y);

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = pt.X,
                    dy = pt.Y,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }


        /// <summary>
        /// We want to use scan codes instead of virtual codes as they are inserted lower into the stack,
        /// but some virtual keys cannot be mapped into scan codes so we need to decide whether to use 
        /// the scan or virtual key
        /// </summary>
        /// <param name="input"></param>
        /// <param name="useScan"></param>
        /// <returns></returns>
        private short CheckKey(ISInputData input, out bool useScan)
        {
            useScan = false;

            if (input.Param2 == 0 || input.Param1 == 91)
                return input.Param1;

            uint mappedScanb = MapVirtualKeyA((uint)input.Param1, MAPVKTYPE.MAPVK_VK_TO_VSC);
            if (mappedScanb != input.Param2)
            {
                ISLogger.Write("Invalid key virtual key:{0} scan code:{1} mapped: {2}", input.Param1, input.Param2, mappedScanb);
                return input.Param2;
            }

            useScan = true;
            return input.Param2;
        }

        static POINT ConvertScreenPointToAbsolutePoint(uint x, uint y)
        {
            // Get current desktop maximum screen resolution-1
            int screenMaxWidth = GetSystemMetrics(0) - 1;
            int screenMaxHeight = GetSystemMetrics(1) - 1;

            double convertedPointX = (x * (65535.0f / screenMaxWidth));
            double convertedPointY = (y * (65535.0f / screenMaxHeight));

            return new POINT
            {
                X = (int)convertedPointX,
                Y = (int)convertedPointY
            };
        }

        private void KeyDownVirtual(short vKey, bool down)
        {
            Input kbIn;
            kbIn.type = 1; //type keyboarrd
            uint flags;
            if (down)
                flags = 0;
            else
                flags = (uint)KeyEventF.KeyUp;

            kbIn.u = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wVk = (ushort)vKey,
                    wScan = 0,
                    dwFlags = flags,
                    dwExtraInfo = IntPtr.Zero,
                    time = 0,
                }
            };

            SendInput(1, new Input[1] { kbIn }, InputSize);
        }

        private void MouseXDown(short button, bool down)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse

            uint flags;
            if (down)
                flags = MOUSEEVENTF_XDOWN;
            else
                flags = MOUSEEVENTF_XUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = (uint)button,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }

        private void KeyDownScan(short scan, bool down)
        {
            Input kbIn;
            kbIn.type = 1; //type keyboarrd
            uint flags;
            if (down)
                flags = (uint)(KeyEventF.Scancode | KeyEventF.KeyDown);
            else
                flags = (uint)(KeyEventF.Scancode | KeyEventF.KeyUp);

            kbIn.u = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wVk = 0,
                    wScan = (ushort)scan,
                    dwFlags = flags,
                    dwExtraInfo = IntPtr.Zero,
                    time = 0,
                }
            };

            SendInput(1, new Input[1] { kbIn }, InputSize);
        }

        private void MoveMouseRelative(short x, short y)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = x,
                    dy = y,
                    dwFlags = MOUSEEVENTF_MOVE
                }
            };

            if (SendInput(1, new Input[1] { mouseIn }, InputSize) != 1)
            {
                ISLogger.Write("Sendinput failed " + Marshal.GetLastWin32Error());
            }
        }

        private void MouseYScroll(short dir)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = (uint)dir,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }

        private void MouseLDown(bool down)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_LEFTDOWN;
            else
                flags = MOUSEEVENTF_LEFTUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }

        private void MouseRDown(bool down)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_RIGHTDOWN;
            else
                flags = MOUSEEVENTF_RIGHTUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }

        private void MouseMDown(bool down)
        {
            Input mouseIn;
            mouseIn.type = 0; //type mouse
            uint flags;
            if (down)
                flags = MOUSEEVENTF_MIDDLEDOWN;
            else
                flags = MOUSEEVENTF_MIDDLEUP;

            mouseIn.u = new InputUnion
            {
                mi = new MouseInput
                {
                    mouseData = 0,
                    time = 0,
                    dx = 0,
                    dy = 0,
                    dwFlags = flags
                }
            };

            SendInput(1, new Input[1] { mouseIn }, InputSize);
        }

        public void Start()
        {
            Running = true;
        }

        public void Stop()
        {
            Running = false;
        }

        public void ResetKeyStates()
        {
            if(((1 << 15) & GetAsyncKeyState(1)) != 0)
                MouseRDown(false);

            for (int i = 1; i < 255; i++)
            {
                if (((1 << 15) & GetAsyncKeyState(i)) != 0)
                {
                    KeyDownVirtual((short)i, false);
                }
            }
        }


        #region win32
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [In] Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vkey);

       

        static int InputSize = Marshal.SizeOf(typeof(Input));

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_XDOWN = 0x0080;
        private const int MOUSEEVENTF_XUP = 0x0100;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;


        const uint MAPVK_VK_TO_VSC = 0x00;
        const uint MAPVK_VSC_TO_VK = 0x01;
        const uint MAPVK_VK_TO_CHAR = 0x02;
        const uint MAPVK_VSC_TO_VK_EX = 0x03;
        const uint MAPVK_VK_TO_VSC_EX = 0x04;

        const int MOUSEEVENTF_MOVE = 0x0001;
        const int MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;

       

        [Flags]
        private enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        private enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008,
        }

        private struct Input
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public readonly HardwareInput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            public readonly uint uMsg;
            public readonly ushort wParamL;
            public readonly ushort wParamH;
        }

        private struct POINT
        {
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X;
            public int Y;
        }

        #endregion
    }
}
