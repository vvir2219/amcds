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
            System.RegisterAbstractionStack(AbstractionId + ".epfd");

            suspected = System.Processes.ToHashSet();
            leader = null;

            UponMessage<EpfdSuspect>((epfdSuspect) => {
                suspected.Add(epfdSuspect.Process);
            });

            UponMessage<EpfdRestore>((epfdRestore) => {
                suspected.Remove(epfdRestore.Process);
            });

            UponCondition(() => leader != Util.Maxrank(System.Processes.Except(suspected)),
            () => {
                leader = Util.Maxrank(System.Processes.Except(suspected));

                Trigger(BuildMessage<EldTrust>(ToParentAbstraction(), (self) => { self.Process = leader; }));
            });
        }
    }
}