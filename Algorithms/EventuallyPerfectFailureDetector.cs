using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class EventuallyPerfectFailureDetector : Algorithm
    {
        private static int delta = 50; // delay increments in ms

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

                        System.EventQueue.RegisterMessage(
                            BuildMessage<EpfdSuspect>(ToParentAbstraction(), (self) => { self.Process = process; })
                        );
                    } else if (alive.Contains(process) && suspected.Contains(process)) {
                        suspected.Remove(process);

                        System.EventQueue.RegisterMessage(
                            BuildMessage<EpfdRestore>(ToParentAbstraction(), (self) => { self.Process = process; })
                        );
                    }

                    System.EventQueue.RegisterMessage(
                        BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                            self.Destination = process;
                            self.Message = BuildMessage<EpfdInternalHeartbeatRequest>(AbstractionId);
                        })
                    );
                }

                alive.Clear();
                RegisterEvent(OnTimeout, delay);
            };

            alive = System.Processes.ToHashSet();
            suspected = new HashSet<ProcessId>();
            RegisterEvent(OnTimeout, delay);

            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var innerMessage = message.PlDeliver.Message;

                switch (innerMessage.Type) {
                    case Message.Types.Type.EpfdInternalHeartbeatRequest: {
                        System.EventQueue.RegisterMessage(
                            BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                                self.Destination = message.PlDeliver.Sender;
                                self.Message = BuildMessage<EpfdInternalHeartbeatReply>(AbstractionId);
                            })
                        );
                        break;
                    }

                    case Message.Types.Type.EpfdInternalHeartbeatReply: {
                        alive.Add(message.PlDeliver.Sender);
                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {innerMessage.Type}");
                };

                return true;
            });
        }

    }
}