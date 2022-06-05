using Protocol;

namespace Project
{
    class EpochChange : Algorithm
    {
        private ProcessId trusted;
        private int lastTimestamp;
        private int timestamp;

        public EpochChange(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            RegisterAbstractionStack(AbstractionId + ".eld");

            trusted = Util.Maxrank(System.Processes);
            lastTimestamp = 0;
            timestamp = System.CurrentProcess.Rank;

            UponMessage<EldTrust>((eldTrust) => {
                trusted = eldTrust.Process;
                if (trusted == System.CurrentProcess) {
                    timestamp += System.Processes.Count;

                    Trigger(
                        BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                            self.Message = BuildMessage<EcInternalNewEpoch>(AbstractionId, (self) => {
                                self.Timestamp = timestamp;
                            });
                        })
                    );
                }
            });

            UponMessage<BebDeliver, EcInternalNewEpoch>((bebDeliver, newEpoch) => {
                if (bebDeliver.Sender == trusted && newEpoch.Timestamp > lastTimestamp) {
                    lastTimestamp = newEpoch.Timestamp;

                    Trigger(
                        BuildMessage<EcStartEpoch>(ToParentAbstraction(), (self) => {
                            self.NewTimestamp = newEpoch.Timestamp;
                            self.NewLeader = bebDeliver.Sender;
                        })
                    );
                } else {
                    Trigger(
                        BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                            self.Destination = bebDeliver.Sender;
                            self.Message = BuildMessage<EcInternalNack>(AbstractionId);
                        })
                    );
                }
            });

            UponMessage<PlDeliver, EcInternalNack>((_, __) => {
                if (trusted == System.CurrentProcess) {
                    timestamp += System.Processes.Count;

                    Trigger(
                        BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                            self.Message = BuildMessage<EcInternalNewEpoch>(AbstractionId, (self) => {
                                self.Timestamp = timestamp;
                            });
                        })
                    );
                }
            });
        }
    }
}