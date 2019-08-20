using System;
using System.Diagnostics;
using System.Net;
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
            client.ConnectionError += Client_ConnectionError;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.Connected += Client_Connected;
            string name = "";
            Console.WriteLine("Enter client name (leave blank for " + Environment.MachineName + ")");
            name = Console.ReadLine();

            if (string.IsNullOrEmpty(name))
            {
                name = Environment.MachineName;
            }

            IPEndPoint address = null;
            while (address == null)
            {
                Console.WriteLine("Enter address:port");
                string addr = Console.ReadLine();

                if(string.IsNullOrEmpty(addr))
                {
                    address = new IPEndPoint(IPAddress.Parse("192.168.0.7"), 4441);
                }
                else
                {
                    IPEndPoint.TryParse(addr, out address);
                }
            }

            Console.WriteLine("Connecting to " + address.ToString());
            client.Connect(address.Address.ToString(), address.Port, name, Guid.NewGuid());

            while (true)
            {
                Console.ReadLine();
            }
        }

        private static void Client_ConnectionFailed(object sender, string e)
        {
            Console.WriteLine("Connection failed: " + e);
        }

        private static void Client_ConnectionError(object sender, string e)
        {
            Console.WriteLine("Connection error: " + e);
        }

        private static void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            Console.WriteLine("Connected to " + e.ToString());
        }

        private static void CurMon_EdgeHit(object sender, Edge e)
        {
            Console.WriteLine("hit " + e);
        }
    }
}
