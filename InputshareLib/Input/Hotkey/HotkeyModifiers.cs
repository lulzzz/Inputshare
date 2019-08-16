using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input.Hotkeys
{
    [FlagsAttribute]
    public enum HotkeyModifiers
    {
        None = 0,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Windows = 0x0008
    }
}
