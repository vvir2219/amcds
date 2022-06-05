using System;
using System.Threading;
using Protocol;

namespace Project
{
    class UniformConsensus : Algorithm
    {
        private Value value = new Value();
        private bool proposed = false,
                     decided = false;
        private int epochTimestamp = 0;
        private ProcessId leader;
        private int newTimestamp = 0;
        private ProcessId newLeader = new ProcessId();

        public UniformConsensus(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            RegisterAbstractionStack(AbstractionId + ".ec");
            leader = Util.Maxrank(System.Processes);
            RegisterEpochConsensus();

            UponMessage<UcPropose>((ucPropose) => {
                value = ucPropose.Value;
            });

            UponMessage<EcStartEpoch>((ecStartEpoch) => {
                newTimestamp = ecStartEpoch.NewTimestamp;
                newLeader = ecStartEpoch.NewLeader;
                Console.WriteLine($"ecstartepoch: {newTimestamp}");
                Trigger(BuildMessage<EpAbort>(ToAbstraction($"ep[{epochTimestamp}]")));
            });

            UponMessage<EpAborted>(
                (epAborted) => epAborted.Ets == epochTimestamp,
                (epAborted) => {
                    epochTimestamp = newTimestamp;
                    leader = newLeader;
                    proposed = false;
                    RegisterEpochConsensus(new EpInternalState {
                        Value = epAborted.Value,
                        ValueTimestamp = epAborted.ValueTimestamp
                    });
                });

            UponCondition(
                () => leader == System.CurrentProcess && value.Defined && proposed == false,
                () => {
                    proposed = true;
                    Trigger(
                        BuildMessage<EpPropose>(ToAbstraction($"ep[{epochTimestamp}]"), (self) =>  {
                            self.Value = value;
                        })
                    );
                });

            UponMessage<EpDecide>(
                (epDecide) => epDecide.Ets == epochTimestamp,
                (epDecide) => {
                    if (! decided) {
                        decided = true;
                        Trigger(
                            BuildMessage<UcDecide>(ToParentAbstraction(), (self) => {
                                self.Value = epDecide.Value;
                            })
                        );
                    }
                });
        }

        private EpochConsensus ep = null;

        public void RegisterEpochConsensus(EpInternalState state = null)
        {
            (new Thread(() => {
                System.RegisterAbstraction(AbstractionId, $"ep[{epochTimestamp}]", CreateEpochConsensus(epochTimestamp, state));
            })).Start();
        }

        public Algorithm CreateEpochConsensus(int epochTimestamp, EpInternalState state = null)
        {
            Console.WriteLine($"EpochTimestamp: {epochTimestamp}");
            // should i try to kill old ep's and take the epochTimestamp for myself?
            var instanceId = $"ep[{epochTimestamp}]";

            if (ep != null) {
                ep.RegisterAction(() => { ep.Running = false; });
            }
            this.epochTimestamp = epochTimestamp;

            ep = new EpochConsensus(
                System,
                instanceId,
                AbstractionId + $".{instanceId}",
                this,
                state ?? new EpInternalState(),
                epochTimestamp);

            return ep;
        }
    }
}