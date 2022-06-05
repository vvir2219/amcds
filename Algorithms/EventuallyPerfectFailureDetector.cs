using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Protocol;

namespace Project
{
    class EventuallyPerfectFailureDetector : Algorithm
    {
        private static int delta = 100; // delay increments in ms

        private HashSet<ProcessId> alive, suspected;
        private int delay = delta;

        private void StartTimer()
        {
            RegisterAction(() => { Trigger(BuildMessage<EpfdTimeout>(AbstractionId)); }, delay);
        }

        public EventuallyPerfectFailureDetector(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            alive = System.Processes.ToHashSet();
            suspected = new HashSet<ProcessId>();
            StartTimer();

            UponMessage<EpfdTimeout>((_) => {
                Console.WriteLine($"{System.CurrentProcess.Owner}-{System.CurrentProcess.Index}: On timeout {delay}ms" +
                                   "\n\talive: "+ string.Join(", ", alive.Select((p) => $"{p.Owner}-{p.Index}")) +
                                   "\n\tsuspected: "+ string.Join(", ", suspected.Select((p) => $"{p.Owner}-{p.Index}")));
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
                StartTimer();
            });

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