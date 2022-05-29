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
                        ToAbstractionId = ToAbstractionId("pl"), // AbstractionId <- this would've been like in the book
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

            // sadly, this is useless as messages as the reference implementation does not respect the
            // book's algorithm
            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var bebDeliver = new Message {
                    Type = Message.Types.Type.BebDeliver,
                    BebDeliver = new BebDeliver {
                        Message = message.PlDeliver.Message,
                        Sender = message.PlDeliver.Sender
                    }
                };

                System.EventQueue.RegisterMessage(bebDeliver, ToAbstractionId());
                return true;
            });
        }
    }
}