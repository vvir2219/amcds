using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;

namespace Project
{
    class Program
    {
        const bool DEBUG = true;

        const string HUB_HOST = "127.0.0.1";
        const int HUB_PORT = 5000;

        const string BASE_HOST = "127.0.0.1";
        const int BASE_PORT = 5010;

        static SystemInfo systemInfo;
        static System system;
        static NetworkManager networkManager;

        static void PrintUsageAndExit()
        {
            Console.WriteLine("Usage: dotnet run owner index");
            global::System.Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2) PrintUsageAndExit();

                string owner = args[0];
                int index = int.Parse(args[1]);

                systemInfo = new SystemInfo {
                    HUB_HOST = HUB_HOST,
                    HUB_PORT = HUB_PORT,
                    SELF_HOST = BASE_HOST,
                    SELF_PORT = BASE_PORT + index,
                    SELF_OWNER = owner,
                    SELF_INDEX = index
                };
                system = new System(systemInfo);
                system.RegisterAlgorithmStack("app");

                networkManager = new NetworkManager(systemInfo, system.EventQueue);

                networkManager.StartListener();
                registerToHub();
            }
            catch
            {
                if (DEBUG)
                    throw;
                else
                    PrintUsageAndExit();
            }
        }

        static void registerToHub()
        {
            var procRegistration = new ProcRegistration {
                Owner = systemInfo.SELF_OWNER,
                Index = systemInfo.SELF_INDEX
            };

            var message = new Message
            {
                Type = Message.Types.Type.ProcRegistration,
                MessageUuid = global::System.Guid.NewGuid().ToString(),
                ProcRegistration = procRegistration
            };

            networkManager.SendNetworkMessage(
                message,
                systemInfo.HUB_HOST,
                systemInfo.HUB_PORT
            );
        }
    }
}
