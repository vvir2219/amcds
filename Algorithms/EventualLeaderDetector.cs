using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class EventualLeaderDetector : Algorithm
    {
        private HashSet<ProcessId> alive;
        private ProcessId leader;

        public EventualLeaderDetector(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            RegisterAbstractionStack(AbstractionId + ".epfd");

            alive = System.Processes.ToHashSet();
            leader = null;

            UponMessage<EpfdSuspect>((epfdSuspect) => {
                alive.Remove(epfdSuspect.Process);
            });

            UponMessage<EpfdRestore>((epfdRestore) => {
                alive.Add(epfdRestore.Process);
            });

            UponCondition(() => alive.Count > 0 && leader != Util.Maxrank(alive),
            () => {
                leader = Util.Maxrank(alive);
                Trigger(BuildMessage<EldTrust>(ToParentAbstraction(), (self) => { self.Process = leader; }));
            });
        }
    }
}