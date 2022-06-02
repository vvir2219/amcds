using System;
using System.ComponentModel.Design;
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
            // messages from hub
            UponMessage<PlDeliver, ProcInitializeSystem>((plDeliver, procInitializeSystem, outer, inner) => {
                System.Processes = procInitializeSystem.Processes.ToList();
                System.SystemId = outer.SystemId;
                Console.WriteLine($"Starting system {System.SystemId} ...");
            });

            UponMessage<PlDeliver, ProcDestroySystem>((plDeliver, procDestroySystem) => {
                Console.WriteLine($"Seek and destroy ...");
            });

            UponMessage<PlDeliver, AppBroadcast>((_, appBroadcast) => {
                var bebMessage = BuildMessage<BebBroadcast>(ToAbstraction("beb"), (message) => {
                        message.Message = BuildMessage<AppValue>(AbstractionId, (self) => {
                            self.Value = appBroadcast.Value;
                        });
                });

                Trigger(bebMessage);
            });

            UponMessage<PlDeliver, AppWrite>((_, appWrite) => {
                var nnarWrite = BuildMessage<NnarWrite>(
                    ToAbstraction($"nnar[{appWrite.Register}]"),
                    (self) => { self.Value = appWrite.Value; });

                Trigger(nnarWrite);
            });

            UponMessage<PlDeliver, AppRead>((_, appRead) => {
                Trigger(
                    BuildMessage<NnarRead>(ToAbstraction($"nnar[{appRead.Register}]"))
                );
            });

            UponMessage<PlDeliver, AppPropose>((_, appPropose) => {
                Trigger(
                    BuildMessage<UcPropose>(ToAbstraction($"uc[{appPropose.Topic}]"), (self) => {
                        self.Value = appPropose.Value;
                    })
                );
            });

            // messages to hub
            UponMessage<BebDeliver>((bebDeliver) => {
                SendToHub(bebDeliver.Message);
            });

            UponMessage<NnarReadReturn>((nnarReadReturn, wrapper) => {
                var (_, register) = Util.DeconstructToInstanceNameAndIndex(System.GetAlgorithm(wrapper.FromAbstractionId).InstanceId);

                SendToHub(
                    BuildMessage<AppReadReturn>(AbstractionId, (self) => {
                        self.Register = register;
                        self.Value = nnarReadReturn.Value;
                    })
                );
            });

            UponMessage<NnarWriteReturn>((nnarWriteReturn, wrapper) => {
                var (_, register) = Util.DeconstructToInstanceNameAndIndex(System.GetAlgorithm(wrapper.FromAbstractionId).InstanceId);

                SendToHub(
                    BuildMessage<AppWriteReturn>(AbstractionId, (self) =>{ self.Register = register; })
                );
            });

            UponMessage<UcDecide>((ucDecide) => {
                SendToHub(
                    BuildMessage<AppDecide>(AbstractionId, (self) => {
                        self.Value = ucDecide.Value;
                    })
                );
            });
        }

        private void SendToHub(Message message)
        {
            var networkMessage = BuildMessage<NetworkMessage>("hub", (self) => {
                self.SenderHost = System.SystemInfo.SELF_HOST;
                self.SenderListeningPort = System.SystemInfo.SELF_PORT;
                self.Message = message;
            });

            System.NetworkManager.SendMessage(
                networkMessage,
                System.SystemInfo.HUB_HOST,
                System.SystemInfo.HUB_PORT
            );
        }
    }
}