using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input
{
    public static class KeyMap
    {
        public static XlibKeySym WinToXlib(WindowsVirtualKey key)
        {
            Enum.TryParse(typeof(XlibKeySym), key.ToString(), out object t);

            if(t == null)
            {
                Console.WriteLine("Failed to translate key {0} to xlib key", key);
                return XlibKeySym.A;
            }

            XlibKeySym k = (XlibKeySym)t;

            Console.WriteLine("Windows {0} ({1}) => Xlib {2} ({3})", key, (int)key, k, (int)k);
            
            return k;

        }
    }
}
