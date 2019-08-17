using InputshareLib.Client;
using System;
using System.Net;
using System.Threading;

namespace InputshareWindows
{
    public static class TestClient
    {
        static ISClient client;

        public static void Run()
        {
            Console.Title = "Inputshare client (Inactive)";
            Console.Clear();

            client = new ISClient(InputshareLibWindows.WindowsDependencies.GetClientDependencies());
            client.ActiveClientChanged += Client_ActiveClientChanged;
            client.ClipboardDataCopied += Client_ClipboardDataCopied;
            client.Connected += Client_Connected;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.ConnectionError += Client_ConnectionError;
            client.Disconnected += Client_Disconnected;
            string clientName = "";
            IPEndPoint address = null;

            Console.WriteLine("Enter client name (leave blank to use machine name)");
            clientName = Console.ReadLine();
            if (string.IsNullOrEmpty(clientName))
                clientName = Environment.MachineName;


            while (address == null)
            {
                Console.Clear();
                Console.WriteLine("Enter server address:port");
                Console.Write("Connect to: ");
                string addr = Console.ReadLine();

                if (string.IsNullOrEmpty(addr))
                    address = new IPEndPoint(IPAddress.Parse("192.168.0.7"), 4441);

                string[] parts = addr.Split(':');
                if (parts.Length != 2)
                    continue;

                if (!IPAddress.TryParse(parts[0], out IPAddress ip))
                    continue;

                if (!int.TryParse(parts[1], out int p))
                    continue;

                if (p < 1 || p > 65535)
                    continue;

                address = new IPEndPoint(ip, p);
            }


            Console.WriteLine("Available commands (non case sensitive):");
            Console.WriteLine("For details on a command, type help followed by the command\n");

            Console.WriteLine("'Connect address:port' - Connects to a server");
            Console.WriteLine("'Disconnect' - Disconnects from server");
            Console.WriteLine();

            client.Connect(address.Address.ToString(), address.Port, clientName);



            while (true)
            {
                if (client.IsConnected)
                    Console.Write("{0}@{1}:", client.ClientName, client.ServerAddress);
                else
                    Console.Write("Inputshare client: ");

                string cmd = Console.ReadLine();
                ExecCmd(cmd);
            }


            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
            Console.Write(".");
            Thread.Sleep(1000);
        }

        private static void Client_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnected");
        }

        private static void Client_ConnectionError(object sender, string error)
        {
            Console.WriteLine("A connection error occurred: " + error);
        }

        private static void Client_ConnectionFailed(object sender, string error)
        {
            Console.WriteLine("Failed to connect to server: " + error);
        }

        private static void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            Console.WriteLine("Connected to {0}", e.ToString());
        }

        private static void Client_ClipboardDataCopied(object sender, EventArgs e)
        {
            //Console.WriteLine("Global clipboard updated");
        }

        private static void Client_ActiveClientChanged(object sender, bool e)
        {
            if (e)
                Console.Title = "Inputshare client (Active)";
            else
                Console.Title = "Inputshare client (Inactive)";
        }

        private static void ExecCmd(string commandLine)
        {
            string[] args = commandLine.Split(' ');


            switch (args[0].ToLower())
            {
                case "disconnect":
                    ExecDisconnect();
                    return;
                case "connect":
                    ExecConnect(args);
                    return;
            }
        }

        private static void ExecDisconnect()
        {
            client.Disconnect();
        }

        private static void ExecConnect(string[] args)
        {
            if (client.IsConnected)
            {
                Console.WriteLine("Client is already connected");
                return;
            }

            if(args.Length != 2)
            {
                Console.WriteLine("Invalid args");
                return;
            }

            string[] parts = args[1].Split(':');
            if(parts.Length != 2)
            {
                Console.WriteLine("Invalid args");
                return;
            }

            try
            {
                client.Connect(parts[0], int.Parse(parts[1]), client.ClientName);
            }catch(Exception ex)
            {
                Console.WriteLine("Could not connect: "+ ex.Message);
            }
        }
    }
}
