using System;
using Protocol;

namespace Project
{
    class App : Algorithm
    {
        public static string InstanceName = "app";

        public App(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            UponMessage(Message.Types.Type.AppBroadcast, (message) => {
                var valueMessage = new Message{
                    ToAbstractionId = AbstractionId,
                    Type = Message.Types.Type.AppValue,
                    AppValue = new AppValue {
                        Value = message.AppBroadcast.Value
                    }
                };

                var bebMessage = new Message{
                    Type = Message.Types.Type.BebBroadcast,
                    BebBroadcast = new BebBroadcast {
                        Message = valueMessage
                    }
                };

                System.EventQueue.RegisterMessage(bebMessage, ToAbstractionId("beb"));
                return true;
            });

            UponMessage(Message.Types.Type.AppValue, (message) => {
                var networkMessage = new Message {
                    SystemId = System.SystemId,
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage {
                        SenderHost = System.SystemInfo.SELF_HOST,
                        SenderListeningPort = System.SystemInfo.SELF_PORT,
                        Message = message
                    }
                };

                System.NetworkManager.SendMessage(
                    networkMessage,
                    System.SystemInfo.HUB_HOST,
                    System.SystemInfo.HUB_PORT
                );
                return true;
            });

            // yea, again, useless
            UponMessage(Message.Types.Type.BebDeliver, (message) => {
                var networkMessage = new Message {
                    SystemId = System.SystemId,
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage {
                        SenderHost = System.SystemInfo.SELF_HOST,
                        SenderListeningPort = System.SystemInfo.SELF_PORT,
                        Message = message.BebDeliver.Message
                    }
                };

                System.NetworkManager.SendMessage(
                    networkMessage,
                    System.SystemInfo.HUB_HOST,
                    System.SystemInfo.HUB_PORT
                );
                return true;
            });
        }

    }
}