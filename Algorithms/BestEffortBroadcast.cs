using System;
using Protocol;

namespace Project
{
    class BestEffortBroadcast : Algorithm
    {
        public static string InstanceName = "beb";

        public BestEffortBroadcast(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            UponMessage(Message.Types.Type.BebBroadcast, (message) => {
                foreach (var process in System.Processes)
                {
                    var plSendMessage = new Message {
                        MessageUuid = Guid.NewGuid().ToString(),
                        ToAbstractionId = ToAbstractionId("pl"),
                        Type = Message.Types.Type.PlSend,
                        PlSend = new PlSend {
                            Destination = process,
                            Message = message.BebBroadcast.Message
                        }
                    };

                    System.EventQueue.RegisterMessage(plSendMessage);
                }
                return true;
            });

            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var bebDeliver = new Message {
                    Type = Message.Types.Type.BebDeliver,
                    ToAbstractionId = ToAbstractionId(), // TODO here could be message.PlDeliver.Message.ToAbstractionId
                    SystemId = message.SystemId,
                    BebDeliver = new BebDeliver {
                        Message = message.PlDeliver.Message,
                        Sender = message.PlDeliver.Sender
                    }
                };

                System.EventQueue.RegisterMessage(bebDeliver);
                return true;
            });
        }
    }
}