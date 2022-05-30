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
                        var suspect = new Message {
                            Type = Message.Types.Type.EpfdSuspect,
                            ToAbstractionId = ToAbstractionId(),
                            EpfdSuspect = new EpfdSuspect {
                                Process = process
                            }
                        };
                        System.EventQueue.RegisterMessage(suspect);
                    } else if (alive.Contains(process) && suspected.Contains(process)) {
                        suspected.Remove(process);
                        var restore = new Message {
                            Type = Message.Types.Type.EpfdRestore,
                            ToAbstractionId = ToAbstractionId(),
                            EpfdRestore = new EpfdRestore {
                                Process = process
                            }
                        };
                        System.EventQueue.RegisterMessage(restore);
                    }

                    var plSend = new Message {
                        Type = Message.Types.Type.PlSend,
                        ToAbstractionId = ToAbstractionId("pl"),
                        PlSend = new PlSend {
                            Destination = process,
                            Message = new Message {
                                Type = Message.Types.Type.EpfdInternalHeartbeatRequest,
                                ToAbstractionId = AbstractionId,
                                EpfdInternalHeartbeatRequest = new EpfdInternalHeartbeatRequest()
                            }
                        }
                    };
                    System.EventQueue.RegisterMessage(plSend);
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
                        var plSend = new Message {
                            Type = Message.Types.Type.PlSend,
                            ToAbstractionId = ToAbstractionId("pl"),
                            PlSend = new PlSend {
                                Destination = message.PlDeliver.Sender,
                                Message = new Message {
                                    Type = Message.Types.Type.EpfdInternalHeartbeatReply,
                                    ToAbstractionId = AbstractionId,
                                    EpfdInternalHeartbeatReply = new EpfdInternalHeartbeatReply()
                                }
                            }
                        };

                        System.EventQueue.RegisterMessage(plSend);
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