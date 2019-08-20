using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Input
{
    public enum XlibKeySym
    {
        Back = 0xff08,
        Tab = 0xff09,
        clear = 0xff0b,
        Return = 0xff0d,
        Shift = 0xffe1, //map generic shift to left shift?
        Control = 0xffe3, //same with control
        Menu = 0xffe9, //Left alt
        Pause = 0xff13,
        CapsLock = 0xffe5,
        //Kana
        //Hanguel
        //Hangul
        //Junja
        //Final
        //Hanja
        //Kanji
        Escape = 0xff1b,
        //Convert
        //NonConver
        //Accept 
        //ModeChange
        Space = 0x0020,
        Prior = 0xff55,
        Next = 0xff56,
        End = 0xff57,
        Home = 0xff50,
        Left = 0x08fb,
        Up = 0x08fc,
        Right = 0x08fd,
        Down = 0x08fe,
        Select = 0xff60,
        Print = 0xff61,
        Execute = 0xff62,
        //Snapshot
        Insert = 0xff63,
        Delete = 0xffff,
        Help = 0xff6a,
        N0 = 0x0030,
        N1 = 0X0031,
        N2 = 0X0032,
        N3 = 0X0033,
        N4 = 0X0034,
        N5 = 0X0035,
        N6 = 0X0036,
        N7 = 0X0037,
        N8 = 0X0038,
        N9 = 0X0039,
        A = 0x0041,
        B = 0x0042,
        C = 0x0043,
        D = 0x0044,
        E = 0x0045,
        F = 0x0046,
        G = 0x0047,
        H = 0x0048,
        I = 0x0049,
        J = 0x004a,
        K = 0x004b,
        L = 0x004c,
        M = 0x004d,
        N = 0x004e,
        O = 0x004f,
        P = 0x0050,
        Q = 0x0051,
        R = 0x0052,
        S = 0x0053,
        T = 0x0054,
        U = 0x0055,
        V = 0x0056,
        W = 0x0057,
        X = 0x0058,
        Y = 0x0059,
        Z = 0x005a,
        //LeftWindows = 0xff5b,
        //RightWindows = 0xff5c,
        //Application = 0xff5d,
        //Sleep
        Numpad0 = 0xffb0,
        Numpad1 = 0xffb1,
        Numpad2 = 0xffb2,
        Numpad3 = 0xffb3,
        Numpad4 = 0xffb4,
        Numpad5 = 0xffb5,
        Numpad6 = 0xffb6,
        Numpad7 = 0xffb7,
        Numpad8 = 0xffb8,
        Numpad9 = 0xffb9,
        Multiply = 0xffaa,
        Add = 0xffab,
        Seperator = 0xffac,
        Subtract = 0xffad,
        Decimal = 0xffae,
        Divide = 0xffaf,
        F1 = 0xffbe,
        F2 = 0xffbf,
        F3 = 0xffc0,
        F4 = 0xffc1,
        F5 = 0xffc2,
        F6 = 0xffc3,
        F7 = 0xffc4,
        F8 = 0xffc5,
        F9 = 0xffc6,
        F10 = 0xffc7,
        F11 = 0xffc8,
        F12 = 0xffc9,
        //F13
        //F14
        //F15
        //F16
        //F17
        //F18
        //F19
        //F20
        //F21
        //F22
        //F23
        //F24
        NumLock = 0xff7f,
        ScrollLock = 0xff14,
        //NEC_Equal
        //EC_Equal
        //Fujitsu_Jisho
        //Fujitsu_Massho
        //Fujitsu_Touroku
        //Fujitsu_Loya
        //Fujitsu_Roya
        LeftShift = 0xffe1,
        RightShift = 0xffe2,
        LeftControl = 0xffe3,
        RightControl = 0xffe4,
        LeftMenu = 0xffe9,
        RightMenu = 0xffea,
        //BrowserBack 
        //BrowserForward
        //BrowserRefresh
        //BrowserStop
        //BrowserSearch
        //BrowserFavorites
        //BrowserHome
        //VolumeMute
        //VolumeDown
        //VolumeUp
        //MediaNextTrack = 0xB0,
        //MediaPrevTrack = 0xB1,
        //MediaStop = 0xB2,
        //MediaPlayPause = 0xB3,
        //LaunchMail = 0xB4,
        //LaunchMediaSelect = 0xB5,
        //LaunchApplication1 = 0xB6,
        //LaunchApplication2 = 0xB7,
        //OEM1 = 0xBA,
        //OEMPlus = 0xBB,
        //OEMComma = 0xBC,
        //OEMMinus = 0xBD,
        //OEMPeriod = 0xBE,
        //OEM2 = 0xBF,
        //OEM3 = 0xC0,
        //OEM4 = 0xDB,
        //OEM5 = 0xDC,
        //OEM6 = 0xDD,
        //OEM7 = 0xDE,
        //OEM8 = 0xDF,
        //OEMAX = 0xE1,
        //OEM102 = 0xE2,
        //ICOHelp = 0xE3,
        //ICO00 = 0xE4,
        //ProcessKey = 0xE5,
        //ICOClear = 0xE6,
        //Packet = 0xE7,
        //OEMReset = 0xE9,
        //OEMJump = 0xEA,
        //OEMPA1 = 0xEB,
        //OEMPA2 = 0xEC,
        //OEMPA3 = 0xED,
        //OEMWSCtrl = 0xEE,
        //OEMCUSel = 0xEF,
        //OEMATTN = 0xF0,
        //OEMFinish = 0xF1,
        //OEMCopy = 0xF2,
        //OEMAuto = 0xF3,
        //OEMENLW = 0xF4,
        //OEMBackTab = 0xF5,
        //ATTN = 0xF6,
        //CRSel = 0xF7,
        //EXSel = 0xF8,
        //EREOF = 0xF9,
        //Play = 0xFA,
        //Zoom = 0xFB,
        //Noname = 0xFC,
        //PA1 = 0xFD,
        //OEMClear = 0xFE
    }
}
