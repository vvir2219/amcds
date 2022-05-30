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
                    var plSend = BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                        self.Destination = process;
                        self.Message = message.BebBroadcast.Message;
                    }, (outer) => { outer.MessageUuid = Guid.NewGuid().ToString(); });

                    System.EventQueue.RegisterMessage(plSend);
                }
                return true;
            });

            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var bebDeliver = BuildMessage<BebDeliver>(ToParentAbstraction(), (self) =>{
                    self.Message = message.PlDeliver.Message;
                    self.Sender = message.PlDeliver.Sender;
                }, (outer) => { outer.SystemId = message.SystemId; });

                System.EventQueue.RegisterMessage(bebDeliver);
                return true;
            });
        }
    }
}