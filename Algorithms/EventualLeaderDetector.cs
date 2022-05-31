using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class EventualLeaderDetector : Algorithm
    {
        private HashSet<ProcessId> suspected;
        private ProcessId leader;

        public EventualLeaderDetector(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            suspected = System.Processes.ToHashSet();
            leader = null;

            UponMessage(Message.Types.Type.EpfdSuspect, (message) => {
                suspected.Add(message.EpfdSuspect.Process);
            });

            UponMessage(Message.Types.Type.EpfdRestore, (message) => {
                suspected.Remove(message.EpfdRestore.Process);
            });

            UponCondition(() => leader != Util.Maxrank(System.Processes.Except(suspected)),
            () => {
                leader = Util.Maxrank(System.Processes.Except(suspected));

                System.EventQueue.RegisterMessage(
                    BuildMessage<EldTrust>(ToParentAbstraction(), (self) => { self.Process = leader; })
                );
            });
        }
    }
}