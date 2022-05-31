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

            UponMessage(Message.Types.Type.EpPropose, (message) => {
                tmpValue = message.EpPropose.Value;

                System.EventQueue.RegisterMessage(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalRead>(AbstractionId);
                    })
                );
            });

            UponMessage(Message.Types.Type.BebDeliver, (message) => {
                var bebDeliver = message.BebDeliver;

                switch (bebDeliver.Message.Type) {
                    case Message.Types.Type.EpInternalRead: {
                        System.EventQueue.RegisterMessage(
                            BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                                self.Destination = bebDeliver.Sender;
                                self.Message = BuildMessage<EpInternalState>(AbstractionId, (self) => {
                                    self.Value = state.Value;
                                    self.ValueTimestamp = state.ValueTimestamp;
                                });
                            })
                        );
                        break;
                    }

                    case Message.Types.Type.EpInternalWrite: {
                        state.ValueTimestamp = epochTimestamp;
                        state.Value = bebDeliver.Message.EpInternalWrite.Value;

                        System.EventQueue.RegisterMessage(
                            BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                                self.Destination = bebDeliver.Sender;
                                self.Message = BuildMessage<EpInternalAccept>(AbstractionId);
                            })
                        );
                        break;
                    }

                    case Message.Types.Type.EpInternalDecided: {
                        System.EventQueue.RegisterMessage(
                            BuildMessage<EpDecide>(ToParentAbstraction(), (self) => {
                                self.Ets = epochTimestamp;
                                self.Value = bebDeliver.Message.EpInternalDecided.Value;
                            })
                        );
                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {bebDeliver.Message.Type}");
                }
            });

            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var plDeliver = message.PlDeliver;

                switch (plDeliver.Message.Type) {
                    case Message.Types.Type.EpInternalState: {
                        states[plDeliver.Sender] = plDeliver.Message.EpInternalState;
                        break;
                    }

                    case Message.Types.Type.EpInternalAccept: {
                        accepted += 1;
                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {plDeliver.Message.Type}");
                }
            });

            UponCondition(() => states.Count > (System.Processes.Count / 2),
            () => {
                var highest = Highest(states.Values);
                if (highest.Value.Defined)
                    tmpValue = highest.Value;
                states.Clear();

                System.EventQueue.RegisterMessage(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalWrite>(AbstractionId, (self) => {
                            self.Value = tmpValue;
                        });
                    })
                );
            });

            UponCondition(() => accepted > (System.Processes.Count / 2),
            () => {
                accepted = 0;
                System.EventQueue.RegisterMessage(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<EpInternalDecided>(AbstractionId, (self) => {
                            self.Value = tmpValue;
                        });
                    })
                );
            });

            UponMessage(Message.Types.Type.EpAbort, (_) => {
                System.EventQueue.RegisterMessage(
                    BuildMessage<EpAborted>(ToParentAbstraction(), (self) => {
                        self.Ets = epochTimestamp;
                        self.Value = state.Value;
                        self.ValueTimestamp = state.ValueTimestamp;
                    })
                );

                Running = false;
            });
        }

        private EpInternalState Highest(IEnumerable<EpInternalState> states)
        {
            return states.Aggregate((maxstate, state) => state.ValueTimestamp > maxstate.ValueTimestamp ? state : maxstate);
        }
    }
}