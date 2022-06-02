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
            UponMessage<BebBroadcast>((bebBroadcast) => {
                foreach (var process in System.Processes)
                    Trigger(
                        BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                            self.Destination = process;
                            self.Message = bebBroadcast.Message;
                        })
                    );
            });

            UponMessage<PlDeliver>((plDeliver, message) => {
                Trigger(
                    BuildMessage<BebDeliver>(ToParentAbstraction(), (self) =>{
                        self.Message = plDeliver.Message;
                        self.Sender = plDeliver.Sender;
                    }, (outer) => { outer.SystemId = message.SystemId; })
                );
            });

        }
    }
}