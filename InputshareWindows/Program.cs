using InputshareLib;
using System;

namespace InputshareWindows
{
    class Program
    {
        static void Main(string[] args)
        {
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = false;
            ISLogger.SetLogFileName("InputshareTest.log");
            ISLogger.PrefixCaller = false;
            ISLogger.PrefixTime = true;


            while (true)
            {
                Console.Clear();
                Console.WriteLine("This is an experimental version of Inputshare, may be unstable :S");
                Console.WriteLine("Log file: " + ISLogger.LogFilePath);
                Console.WriteLine("Press S to run server, C to run client");

                ConsoleKeyInfo key = Console.ReadKey();

                if(key.Key == ConsoleKey.S)
                {
                    TestServer.Run();
                    continue;
                }else if(key.Key == ConsoleKey.C)
                {
                    TestClient.Run();
                    continue;
                }

            }
            


        }
    }
}
