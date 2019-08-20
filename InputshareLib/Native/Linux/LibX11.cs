using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace InputshareLib.Native.Linux
{
    public static class LibX11
    {
        [DllImport("libX11.so.6")]
        public static extern IntPtr XOpenDisplay(int scr);
        [DllImport("libX11.so.6")]
        public static extern void XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern uint XKeysymToKeycode(IntPtr display, IntPtr keysym);

        public static uint XKeysymToKeycode(IntPtr display, int keysym) { return XKeysymToKeycode(display, (IntPtr)keysym); }

        [DllImport("libX11.so.6")]
        public static extern IntPtr XFlush(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so.6")]
        public static extern void XWarpPointer(IntPtr display,
                                               IntPtr src_w,
                                               IntPtr dest_w,
                                               int src_x,
                                               int src_y,
                                               uint src_width,
                                               uint src_height,
                                               int dest_x,
                                               int dest_y);

        [DllImport("libX11.so.6")]
        public extern static bool XQueryPointer(IntPtr display,
                                                IntPtr window,
                                                out IntPtr root,
                                                out IntPtr child,
                                                out int root_x,
                                                out int root_y,
                                                out int win_x,
                                                out int win_y,
                                                out int keys_buttons);

        [DllImport("libX11.so.6")]
        public static extern bool XGetWindowAttributes(IntPtr display, IntPtr w, out XWindowAttributes window_attributes_return);

        [StructLayout(LayoutKind.Sequential)]
        public struct XWindowAttributes
        {
            public int x;
            public int y;
            public int width;
            public int height;
            public int border_width;
            public int depth;
            public IntPtr visual;
            public IntPtr root;
            public int c_class;
            public Gravity bit_gravity;
            public Gravity win_gravity;
            public int backing_store;
            public IntPtr backing_planes;
            public IntPtr backing_pixel;
            public bool save_under;
            public IntPtr colormap;
            public bool map_installed;
            public int map_state;
            public IntPtr all_event_masks;
            public IntPtr your_event_mask;
            public IntPtr do_not_propagate_mask;
            public bool override_direct;
            public IntPtr screen;
        }

        public enum Gravity
        {
            ForgetGravity = 0,
            NorthWestGravity = 1,
            NorthGravity = 2,
            NorthEastGravity = 3,
            WestGravity = 4,
            CenterGravity = 5,
            EastGravity = 6,
            SouthWestGravity = 7,
            SouthGravity = 8,
            SouthEastGravity = 9,
            StaticGravity = 10
        }
    }
}
