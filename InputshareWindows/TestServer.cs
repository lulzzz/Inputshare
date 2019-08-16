using InputshareLib;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Server;
using InputshareLibWindows;
using InputshareLibWindows.Cursor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputshareWindows
{
    public static class TestServer
    {
        static ISServer server;
        static Dictionary<string, Action<string[]>> commandDictionary = new Dictionary<string, Action<string[]>>();

        public static void Run()
        {
            Console.Clear();

            int port = 0;

            do
            {
                Console.WriteLine("Enter port to start server: ");
                string ptrStr = Console.ReadLine();

                int.TryParse(ptrStr, out port);
                Console.Clear();
            } while (port < 1 || port > 65535);

            Console.Clear();

            server = new ISServer(WindowsDependencies.GetServerDependencies());
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.InputClientSwitched += Server_InputClientSwitched;
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;
            try
            {
                server.Start(port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start server: " + ex.Message);
            }

            PrintHelp();

            while (server.Running)
            {
                Console.Write("Inputshare server: ");
                ExecCmd(Console.ReadLine());
            }

            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            return;
        }

        private static void Server_Stopped(object sender, EventArgs e)
        {
            Console.WriteLine("Server stopped");
        }

        private static void Server_Started(object sender, EventArgs e)
        {
            Console.WriteLine("Server started on {0}", server.BoundAddress.ToString());
        }

        private static void Server_InputClientSwitched(object sender, ClientInfo e)
        {
            Console.Title = "Inputshare server - " + e.Name;
        }

        private static void Server_ClientDisconnected(object sender, ClientInfo client)
        {
            Console.WriteLine("Client {0} disconnected", client.Name);
        }

        private static void Server_ClientConnected(object sender, ClientInfo client)
        {
            Console.WriteLine("Client {0} connected from {1}", client.Name, client.ClientAddress);
        }

        private static void ExecCmd(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                Console.WriteLine("Invalid command...");
                return;
            }

            string[] args = commandLine.ToLower().Split(' ');

            switch (args[0])
            {
                case "set":
                    {
                        if (args.Length != 4)
                        {
                            Console.WriteLine("Invalid args");
                            return;
                        }
                        else
                        {
                            Cmd_Set(args[1], args[2], args[3]);
                            return;
                        }
                    }
                case "assign":
                    {
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Invalid args");
                            return;
                        }
                        else
                        {
                            Cmd_Assign(args[1]);
                            return;
                        }
                    }
                case "setmousebuffered":
                    if (args.Length != 2)
                    {
                        Console.WriteLine("Invalid args");
                        return;
                    }
                    else
                    {
                        Cmd_SetMouseBuffered(args[1]);
                        return;
                    }

                case "setmouserealtime":
                    {
                        Cmd_SetMouseRealtime();
                        return;
                    }

                case "stop":
                    Cmd_Stop();
                    return;

                case "start":
                    if(args.Length != 2)
                    {
                        Console.WriteLine("Invalid args");
                        return;
                    }
                    else
                    {
                        Cmd_Start(args[1]);
                        return;
                    }

                case "help":
                    PrintHelp();
                    return;
                case "list":
                    Cmd_List();
                    return;
                case "clear":
                    Console.Clear();
                    return;
                default:
                    Console.WriteLine("Invalid command");
                    return;

            }
        }

        private static void Cmd_List()
        {
            ClientInfo[] clients = server.GetAllClients();

            Console.WriteLine("\nCurrent input client: " + GetInputClient().Name);

            foreach(var client in clients)
            {
                Console.WriteLine();
                Console.WriteLine(client.Name + " | " + client.ClientHotkey?.ToString() + " | " + client.DisplayConf.VirtualBounds.Width + ":" + client.DisplayConf.VirtualBounds.Height  + " | " + client.ClientAddress.ToString());

                if (client.LeftClient != null)
                    Console.WriteLine("Left client: " + client.LeftClient.Name);
                if (client.RightClient != null)
                    Console.WriteLine("right client: " + client.RightClient.Name);
                if (client.BottomClient != null)
                    Console.WriteLine("bottom client: " + client.BottomClient.Name);
                if (client.TopClient != null)
                    Console.WriteLine("top client: " + client.TopClient.Name);
            }

            Console.WriteLine();
        }

        private static ClientInfo GetInputClient()
        {
            foreach(var client in server.GetAllClients())
            {
                if (client.InputClient)
                    return client;
            }
            return null; 
        }

        private static void Cmd_Assign(string arg1)
        {
            ClientInfo clientA = GetClientFromName(arg1);

            if (clientA == null)
            {
                Console.WriteLine("Invalid client.");
                ListClients();
                return;
            }

            Console.WriteLine("Hotkey to assign to " + clientA.Name);

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);



            HotkeyModifiers mods = 0;
            if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                mods |= HotkeyModifiers.Ctrl;
            if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
                mods |= HotkeyModifiers.Alt;
            if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                mods |= HotkeyModifiers.Shift;

            try
            {
                server.SetHotkeyForClient(clientA, new Hotkey((WindowsVirtualKey)keyInfo.Key, mods));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set hotkey: " + ex.Message);
            }
        }

        private static void Cmd_Start(string arg1)
        {
            if (server.Running)
            {
                Console.WriteLine("Server already running");
                return;
            }

            int.TryParse(arg1, out int port);

            if(port < 1 || port > 65535)
            {
                Console.WriteLine("Invalid port");
                return;
            }

            server.Start(port);
        }

        private static void Cmd_Stop()
        {
            if (!server.Running)
            {
                Console.WriteLine("Server not running");
                return;
            }

            server.Stop();
        }

        private static void Cmd_SetMouseBuffered(string arg)
        {
            if (!server.Running)
            {
                Console.WriteLine("Server not running");
                return;
            }

            int.TryParse(arg, out int bufferRate);

            if (bufferRate < 1)
            {
                Console.WriteLine("Invalid arg");
                return;
            }

            server.SetMouseInputMode(MouseInputMode.Buffered, bufferRate);
        }

        private static void Cmd_SetMouseRealtime()
        {
            if (!server.Running)
            {
                Console.WriteLine("Server not running");
                return;
            }

            server.SetMouseInputMode(MouseInputMode.Realtime);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("---------------------------------------------------------------------");
            Console.WriteLine("Available commands (non case sensitive):");
            Console.WriteLine("'List' - Lists all clients");
            Console.WriteLine("'Start X' - Starts the server on port X");
            Console.WriteLine("'Stop' - Stops the server");
            Console.WriteLine("'Set X sideof Y' - Sets a client to the edge of another client");
            Console.WriteLine("'Assign client' - Assigns a hotkey to a client, hotkey entered via console");
            Console.WriteLine("'SetMouseBuffered X' - Sets the server to send mouse updates X times a second");
            Console.WriteLine("'SetMouseRealtime' - Sets the server to send mouse updates as fast as possible (1-1 input)");
            Console.WriteLine("'Clear' - Clears the console");
            Console.WriteLine("'Help' - shows this message");
            Console.WriteLine("---------------------------------------------------------------------");
        }

        private static ClientInfo GetClientFromName(string name)
        {
            foreach (var client in server.GetAllClients())
            {
                if (client.Name.ToLower().Contains(name.ToLower()))
                    return client;
            }

            return null;
        }

        //SET X sideof Y
        private static void Cmd_Set(string arg1, string arg2, string arg3)
        {
            if (!server.Running)
            {
                Console.WriteLine("Server not running");
                return;
            }

            ClientInfo[] clients = server.GetAllClients();

            ClientInfo clientA = null;
            ClientInfo clientB = null;

            foreach (var client in clients)
            {
                if (arg1 != "none")
                    if (client.Name.ToLower().Contains(arg1))
                        clientA = client;

                if (client.Name.ToLower().Contains(arg3))
                    clientB = client;
            }

            if ((clientA == null && arg1 != "none") || clientB == null)
            {
                Console.WriteLine("Invalid client");
                ListClients();
                return;
            }

            if (clientA == clientB)
            {
                Console.WriteLine("Cannot set X sideof X");
                return;
            }



            if (Enum.TryParse(typeof(Edge), arg2, true, out object side))
            {
                Edge edge = (Edge)side;

                if (arg1 == "none")
                {
                    server.RemoveClientEdge(clientB, edge);
                }
                else
                {
                    server.SetClientEdge(clientA, edge, clientB);
                }

            }
            else
            {
                Console.WriteLine("Invalid side of client");
                Console.WriteLine("Sides: left, right, top, bottom, none");
                return;
            }

        }

        private static void ListClients()
        {
            Console.WriteLine("Connected clients:");

            foreach (var client in server.GetAllClients())
            {
                Console.WriteLine(client.Name + " | " + client.ClientAddress);
            }
        }
    }
}
