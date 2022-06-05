using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class EpochConsensus : Algorithm
    {
        private EpInternalState state;
        private Value tmpValue = new Value();
        private Dictionary<ProcessId, EpInternalState> states = new Dictionary<ProcessId, EpInternalState>();
        private int accepted = 0;
        private int epochTimestamp;

        public EpochConsensus(System system, string instanceId, string abstractionId, Algorithm parent, EpInternalState _state, int _epochTimestamp)
            : base(system, instanceId, abstractionId, parent)
        {
            state = _state;
            epochTimestamp = _epochTimestamp;

            UponMessage<EpPropose>((epPropose) => {
                tmpValue = epPropose.Value;

                Trigger(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalRead>(AbstractionId);
                    })
                );
            });

            UponMessage<BebDeliver, EpInternalRead>((bebDeliver, _) => {
                Trigger(
                    BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                        self.Destination = bebDeliver.Sender;
                        self.Message = BuildMessage<EpInternalState>(AbstractionId, (self) => {
                            self.Value = state.Value;
                            self.ValueTimestamp = state.ValueTimestamp;
                        });
                    })
                );
            });

            UponMessage<PlDeliver, EpInternalState>((plDeliver, epInternalState) => {
                states[plDeliver.Sender] = epInternalState;
            });

            UponCondition(() => states.Count > (System.Processes.Count / 2),
            () => {
                var highest = Highest(states.Values);
                if (highest.Value != null && highest.Value.Defined)
                    tmpValue = highest.Value;
                states.Clear();

                Trigger(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalWrite>(AbstractionId, (self) => {
                            self.Value = tmpValue;
                        });
                    })
                );
            });

            UponMessage<BebDeliver, EpInternalWrite>((bebDeliver, epInternalWrite) => {
                state.ValueTimestamp = epochTimestamp;
                state.Value = epInternalWrite.Value;

                Trigger(
                    BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>
                    {
                        self.Destination = bebDeliver.Sender;
                        self.Message = BuildMessage<EpInternalAccept>(AbstractionId);
                    })
                );
            });

            UponMessage<PlDeliver, EpInternalAccept>((_, __) => {
                accepted += 1;
            });

            UponCondition(() => accepted > (System.Processes.Count / 2),
            () => {
                accepted = 0;
                Trigger(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalDecided>(AbstractionId, (self) => {
                            self.Value = tmpValue;
                        });
                    })
                );
            });

            UponMessage<BebDeliver, EpInternalDecided>((_, epInternalDecided) => {
                Trigger(
                    BuildMessage<EpDecide>(ToParentAbstraction(), (self) =>
                    {
                        self.Ets = epochTimestamp;
                        self.Value = epInternalDecided.Value;
                    })
                );
            });

            UponMessage<EpAbort>((_) => {
                Trigger(
                    BuildMessage<EpAborted>(ToParentAbstraction(), (self) => {
                        self.Ets = epochTimestamp;
                        self.Value = state.Value;
                        self.ValueTimestamp = state.ValueTimestamp;
                    })
                );

                RegisterAction(() => { Running = false; });
            });
        }

        private EpInternalState Highest(IEnumerable<EpInternalState> states)
        {
            return states.Aggregate((maxstate, state) => state.ValueTimestamp > maxstate.ValueTimestamp ? state : maxstate);
        }
    }
}