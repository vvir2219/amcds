using System;
using System.Diagnostics;
using System.Linq;
using Protocol;

namespace Project
{
    class ProcManager : Algorithm
    {
        public ProcManager(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            UponMessage(Message.Types.Type.ProcInitializeSystem, (message) => {
                system.Processes = message.ProcInitializeSystem.Processes.ToList();
                system.SystemId = message.SystemId;
                Console.WriteLine($"Starting system {message.SystemId} ...");
                return true;
            });

            UponMessage(Message.Types.Type.ProcDestroySystem, (message) => {
                // TODO destroy the system
                Console.WriteLine($"Stopping ...");
                return true;
            });
        }
    }
}