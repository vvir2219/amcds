using System;
using System.Linq;
using Protocol;

namespace Project
{
    class PerfectLink : Algorithm
    {
        public static string InstanceName = "pl";

        public PerfectLink(System system, string instanceId, string abstractionId) : base(system, instanceId, abstractionId)
        {
            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var innerMessage = message.PlDeliver.Message;
                var innerTo = innerMessage.ToAbstractionId;

                if (innerTo == null || innerTo == string.Empty) {
                    switch (innerMessage.Type) {
                        case Message.Types.Type.ProcInitializeSystem:
                            system.Processes = innerMessage.ProcInitializeSystem.Processes.ToList();
                            Console.WriteLine($"Starting system {message.SystemId} ...");
                            return true;

                        default:
                            return false;
                    }
                } else {
                    System.EventQueue.RegisterMessage(innerMessage);
                    return true;
                }
            });
        }
    }
}