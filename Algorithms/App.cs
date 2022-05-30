using System;
using System.Linq;
using Protocol;

namespace Project
{
    class App : Algorithm
    {
        public static string InstanceName = "app";

        public App(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            // messages directly from hub
            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var innerMessage = message.PlDeliver.Message;
                switch (innerMessage.Type) {
                    case Message.Types.Type.ProcInitializeSystem: {
                        System.Processes = innerMessage.ProcInitializeSystem.Processes.ToList();
                        System.SystemId = message.SystemId;
                        Console.WriteLine($"Starting system {message.SystemId} ...");
                        break;
                    }

                    case Message.Types.Type.ProcDestroySystem: {
                        Console.WriteLine($"Stopping ...");
                        break;
                    }

                    case Message.Types.Type.AppBroadcast: {
                        var bebMessage = BuildMessage<BebBroadcast>(ToAbstraction("beb"), (message) => {
                                message.Message = BuildMessage<AppValue>(AbstractionId, (self) => {
                                    self.Value = innerMessage.AppBroadcast.Value;
                                });
                        });

                        System.EventQueue.RegisterMessage(bebMessage);
                        break;
                    }

                    case Message.Types.Type.AppWrite: {
                        var nnarWrite = BuildMessage<NnarWrite>(
                            ToAbstraction($"nnar[{innerMessage.AppWrite.Register}]"),
                            (self) => { self.Value = innerMessage.AppWrite.Value; });

                        System.EventQueue.RegisterMessage(nnarWrite);
                        break;
                    }

                    case Message.Types.Type.AppRead: {
                        var nnarRead = BuildMessage<NnarRead>(ToAbstraction($"nnar[{innerMessage.AppRead.Register}]"));

                        System.EventQueue.RegisterMessage(nnarRead);
                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {innerMessage.Type}");
                }
                return true;
            });

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

            UponMessage(Message.Types.Type.NnarReadReturn, (message) => {
                var (_, register) = Util.DeconstructToInstanceNameAndIndex(System.GetAlgorithm(message.FromAbstractionId).InstanceId);

                var networkMessage = new Message {
                    SystemId = System.SystemId,
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage {
                        SenderHost = System.SystemInfo.SELF_HOST,
                        SenderListeningPort = System.SystemInfo.SELF_PORT,
                        Message = BuildMessage<AppReadReturn>(AbstractionId, (self) =>{
                            self.Register = register;
                            self.Value = message.NnarReadReturn.Value;
                        }),
                    }
                };

                System.NetworkManager.SendMessage(
                    networkMessage,
                    System.SystemInfo.HUB_HOST,
                    System.SystemInfo.HUB_PORT
                );
                return true;
            });

            UponMessage(Message.Types.Type.NnarWriteReturn, (message) => {
                var (_, register) = Util.DeconstructToInstanceNameAndIndex(System.GetAlgorithm(message.FromAbstractionId).InstanceId);

                var networkMessage = new Message {
                    SystemId = System.SystemId,
                    Type = Message.Types.Type.NetworkMessage,
                    NetworkMessage = new NetworkMessage {
                        SenderHost = System.SystemInfo.SELF_HOST,
                        SenderListeningPort = System.SystemInfo.SELF_PORT,
                        Message = BuildMessage<AppWriteReturn>(AbstractionId, (self) => {
                            self.Register = register;
                        })
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