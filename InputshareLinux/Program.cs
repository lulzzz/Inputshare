using System;
using System.Diagnostics;
using InputshareLib;
using InputshareLib.Client;
using InputshareLib.Clipboard;
using InputshareLib.Cursor;
using InputshareLib.Displays;
using InputshareLib.DragDrop;
using InputshareLib.Output;

namespace InputshareLinux
{
    class Program
    {
        static ISClient client;

        static void Main(string[] args)
        {
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;
            ISLogger.SetLogFileName("InputshareLinux.log");

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            ISLogger.Write("Priority is now " + Process.GetCurrentProcess().PriorityClass);

            Console.WriteLine("This is an experimental linux client for inputshare");
            Console.Title = "Inputshare client";
            Console.WriteLine("Log file: " + ISLogger.LogFilePath);

            ClientDependencies deps = new ClientDependencies
            {
                clipboardManager = new NullClipboardManager(),
                cursorMonitor = new XlibCursorMonitor(),
                displayManager = new XlibDisplayManager(),
                dragDropManager = new NullDragDropManager(),
                outputManager = new XlibOutputManager()
            };

            client = new ISClient(deps);
            client.Connect("192.168.0.7", 4441, Environment.MachineName);
            Console.ReadLine();
        }

        private static void Client_Connected(object sender, System.Net.IPEndPoint e)
        {

        }

        private static void CurMon_EdgeHit(object sender, Edge e)
        {
            Console.WriteLine("hit " + e);
        }
    }
}
