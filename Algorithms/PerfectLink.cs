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
            UponMessage<NetworkMessage>((networkMessage, message) => {
                Trigger(
                    BuildMessage<PlDeliver>(
                        ToParentAbstraction(),
                        (self) => {
                            self.Sender = System.GetProcessByHostAndPort(networkMessage.SenderHost, networkMessage.SenderListeningPort);
                            self.Message = networkMessage.Message;
                        },
                        (outerSelf) => {
                            outerSelf.SystemId = message.SystemId;
                            outerSelf.MessageUuid = message.MessageUuid;
                        }
                    )
                );
            });

            UponMessage<PlSend>((plSend, message) => {
                var networkMessage = BuildMessage<NetworkMessage>(
                    message.ToAbstractionId,
                    (self) => {
                        self.SenderHost = System.SystemInfo.SELF_HOST;
                        self.SenderListeningPort = System.SystemInfo.SELF_PORT;
                        self.Message = plSend.Message;
                    });

                System.NetworkManager.SendMessage(
                    networkMessage,
                    plSend.Destination.Host,
                    plSend.Destination.Port
                );
            });
        }
    }
}