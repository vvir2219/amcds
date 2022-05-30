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
                    case Message.Types.Type.ProcInitializeSystem:
                        System.Processes = innerMessage.ProcInitializeSystem.Processes.ToList();
                        System.SystemId = message.SystemId;
                        Console.WriteLine($"Starting system {message.SystemId} ...");
                        break;

                    case Message.Types.Type.ProcDestroySystem:
                        Console.WriteLine($"Stopping ...");
                        break;

                    case Message.Types.Type.AppBroadcast:
                        var valueMessage = new Message{
                            ToAbstractionId = "app",
                            Type = Message.Types.Type.AppValue,
                            AppValue = new AppValue {
                                Value = innerMessage.AppBroadcast.Value
                            }
                        };

                        var bebMessage = new Message{
                            Type = Message.Types.Type.BebBroadcast,
                            ToAbstractionId = ToAbstractionId("beb"),
                            BebBroadcast = new BebBroadcast {
                                Message = valueMessage
                            }
                        };

                        System.EventQueue.RegisterMessage(bebMessage);
                        break;

                    case Message.Types.Type.AppWrite:
                        var nnarWrite = new Message {
                            Type = Message.Types.Type.NnarWrite,
                            ToAbstractionId = ToAbstractionId($"nnar[{innerMessage.AppWrite.Register}]"),
                            NnarWrite = new NnarWrite {
                                Value = innerMessage.AppWrite.Value
                            }
                        };

                        System.EventQueue.RegisterMessage(nnarWrite);
                        break;

                    case Message.Types.Type.AppRead: {
                        var nnarRead = new Message {
                            Type = Message.Types.Type.NnarRead,
                            ToAbstractionId = ToAbstractionId($"nnar[{innerMessage.AppRead.Register}]"),
                            NnarRead = new NnarRead()
                        };

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
                        Message = new Message {
                            Type = Message.Types.Type.AppReadReturn,
                            ToAbstractionId = AbstractionId,
                            AppReadReturn = new AppReadReturn {
                                Register = register,
                                Value = message.NnarReadReturn.Value
                            }
                        }
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
                        Message = new Message {
                            Type = Message.Types.Type.AppWriteReturn,
                            ToAbstractionId = AbstractionId,
                            AppWriteReturn = new AppWriteReturn {
                                Register = register
                            }
                        }
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