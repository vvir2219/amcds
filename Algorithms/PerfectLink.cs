using System;
using System.Linq;
using Protocol;

namespace Project
{
    class PerfectLink : Algorithm
    {
        public static string InstanceName = "pl";

        public PerfectLink(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var innerMessage = message.PlDeliver.Message;
                innerMessage.SystemId = message.SystemId;

                System.EventQueue.RegisterMessage(innerMessage);
                return true;
            });

            UponMessage(Message.Types.Type.PlSend, (message) => {
                var networkMessage = new Message {
                    SystemId = System.SystemId,
                    ToAbstractionId = message.ToAbstractionId,
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage {
                        SenderHost = System.SystemInfo.SELF_HOST,
                        SenderListeningPort = System.SystemInfo.SELF_PORT,
                        Message = message.PlSend.Message
                    }
                };
                System.NetworkManager.SendMessage(
                    networkMessage,
                    message.PlSend.Destination.Host,
                    message.PlSend.Destination.Port
                );
                return true;
            });
        }
    }
}