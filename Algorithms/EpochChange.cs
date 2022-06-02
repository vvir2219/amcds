// using Protocol;

// namespace Project
// {
//     class EpochChange : Algorithm
//     {
//         private ProcessId trusted;
//         private int lastTimestamp;
//         private int timestamp;

//         public EpochChange(System system, string instanceId, string abstractionId, Algorithm parent)
//             : base(system, instanceId, abstractionId, parent)
//         {
//             trusted = System.CurrentProcess;
//             lastTimestamp = 0;
//             timestamp = System.CurrentProcess.Rank;

//             UponMessage(Message.Types.Type.EldTrust, (message) => {
//                 trusted = message.EldTrust.Process;
//                 if (trusted == System.CurrentProcess) {
//                     timestamp += System.Processes.Count;

//                     System.EventQueue.RegisterMessage(
//                         BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
//                             self.Message = BuildMessage<EcInternalNewEpoch>(AbstractionId, (self) => {
//                                 self.Timestamp = timestamp;
//                             });
//                         })
//                     );
//                 }
//             });

//             UponMessage(Message.Types.Type.BebDeliver, (message) => {
//                 var bebDeliver = message.BebDeliver;
//                 var newEpoch = bebDeliver.Message.EcInternalNewEpoch;

//                 if (bebDeliver.Sender == trusted && newEpoch.Timestamp > lastTimestamp) {
//                     lastTimestamp = newEpoch.Timestamp;

//                     System.EventQueue.RegisterMessage(
//                         BuildMessage<EcStartEpoch>(ToParentAbstraction(), (self) => {
//                             self.NewTimestamp = newEpoch.Timestamp;
//                             self.NewLeader = bebDeliver.Sender;
//                         })
//                     );
//                 } else {
//                     System.EventQueue.RegisterMessage(
//                         BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
//                             self.Destination = bebDeliver.Sender;
//                             self.Message = BuildMessage<EcInternalNack>(AbstractionId);
//                         })
//                     );
//                 }
//             });

//             UponMessage(Message.Types.Type.PlDeliver, (message) => { // NACK
//                 if (trusted == System.CurrentProcess) {
//                     timestamp += System.Processes.Count;

//                     System.EventQueue.RegisterMessage(
//                         BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
//                             self.Message = BuildMessage<EcInternalNewEpoch>(AbstractionId, (self) => {
//                                 self.Timestamp = timestamp;
//                             });
//                         })
//                     );
//                 }
//             });
//         }
//     }
// }