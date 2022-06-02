using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class EventuallyPerfectFailureDetector : Algorithm
    {
        private static int delta = 100; // delay increments in ms

        private HashSet<ProcessId> alive, suspected;
        private int delay = delta;

        private Action OnTimeout;

        public EventuallyPerfectFailureDetector(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            OnTimeout = () => {
                if (alive.Intersect(suspected).Count() > 0)
                    delay += delta;

                foreach (var process in System.Processes) {
                    if (! (alive.Contains(process) || suspected.Contains(process))) {
                        suspected.Add(process);

                        Trigger(
                            BuildMessage<EpfdSuspect>(ToParentAbstraction(), (self) => { self.Process = process; })
                        );
                    } else if (alive.Contains(process) && suspected.Contains(process)) {
                        suspected.Remove(process);

                        Trigger(
                            BuildMessage<EpfdRestore>(ToParentAbstraction(), (self) => { self.Process = process; })
                        );
                    }

                    Trigger(
                        BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                            self.Destination = process;
                            self.Message = BuildMessage<EpfdInternalHeartbeatRequest>(AbstractionId);
                        })
                    );
                }

                alive.Clear();
                RegisterAction(OnTimeout, delay);
            };

            alive = System.Processes.ToHashSet();
            suspected = new HashSet<ProcessId>();
            RegisterAction(OnTimeout, delay);

            UponMessage<PlDeliver, EpfdInternalHeartbeatRequest>((plDeliver, _) => {
                Trigger(
                    BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                        self.Destination = plDeliver.Sender;
                        self.Message = BuildMessage<EpfdInternalHeartbeatReply>(AbstractionId);
                    })
                );
            });

            UponMessage<PlDeliver, EpfdInternalHeartbeatReply>((plDeliver, _) => {
                alive.Add(plDeliver.Sender);
            });
        }
    }
}