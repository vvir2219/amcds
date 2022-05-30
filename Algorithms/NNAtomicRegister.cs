using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;

namespace Project
{
    class NNAtomicRegister : Algorithm
    {
        private int timestamp = 0,
                    writerRank = 0;
        private Value value = new Value();
        private int acks = 0;
        private Value writeval = new Value();
        private int readid = 0;
        private Dictionary<ProcessId, Tuple<int, int, Value>> readlist = new Dictionary<ProcessId, Tuple<int, int, Value>>();
        private Value readval = new Value();
        private bool reading = false;

        public NNAtomicRegister(System system, string instanceId, string abstractionId, Algorithm parent)
            : base(system, instanceId, abstractionId, parent)
        {
            UponMessage(Message.Types.Type.NnarWrite, (message) => {
                readid += 1;
                writeval = message.NnarWrite.Value;
                acks = 0;
                readlist.Clear();

                System.EventQueue.RegisterMessage(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<NnarInternalRead>(AbstractionId, (self) => { self.ReadId = readid; });
                    })
                );
                return true;
            });

            UponMessage(Message.Types.Type.NnarRead, (message) => {
                readid += 1;
                acks = 0;
                readlist.Clear();
                reading = true;

                System.EventQueue.RegisterMessage(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<NnarInternalRead>(AbstractionId, (self) => { self.ReadId = readid; });
                    })
                );
                return true;
            });

            UponMessage(Message.Types.Type.BebDeliver, (message) => {
                var innerMessage = message.BebDeliver.Message;

                switch (innerMessage.Type) {
                    case Message.Types.Type.NnarInternalRead: {
                        System.EventQueue.RegisterMessage(
                            BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                                self.Destination = message.BebDeliver.Sender;
                                self.Message = BuildMessage<NnarInternalValue>(AbstractionId, (self) => {
                                    self.ReadId = innerMessage.NnarInternalRead.ReadId;
                                    self.Timestamp = timestamp;
                                    self.WriterRank = writerRank;
                                    self.Value = value;
                                });
                            })
                        );
                        break;
                    }

                    case Message.Types.Type.NnarInternalWrite: {
                        var nnarInternalWrite = innerMessage.NnarInternalWrite;
                        if (nnarInternalWrite.Timestamp > timestamp ||
                            (nnarInternalWrite.Timestamp == timestamp && nnarInternalWrite.WriterRank > writerRank)) {
                            timestamp = nnarInternalWrite.Timestamp;
                            writerRank = nnarInternalWrite.WriterRank;
                            value = nnarInternalWrite.Value;
                        }

                        System.EventQueue.RegisterMessage(
                            BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                                self.Destination = message.BebDeliver.Sender;
                                self.Message = BuildMessage<NnarInternalAck>(AbstractionId, (self) => {
                                    self.ReadId = nnarInternalWrite.ReadId;
                                });
                            })
                        );
                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {innerMessage.Type}");
                };

                return true;
            });

            UponMessage(Message.Types.Type.PlDeliver, (message) => {
                var innerMessage = message.PlDeliver.Message;

                switch (innerMessage.Type) {
                    case Message.Types.Type.NnarInternalValue: {
                        if (readid != innerMessage.NnarInternalValue.ReadId) return false;

                        readlist[message.PlDeliver.Sender] = Tuple.Create(
                                innerMessage.NnarInternalValue.Timestamp,
                                innerMessage.NnarInternalValue.WriterRank,
                                innerMessage.NnarInternalValue.Value
                            );

                        if (readlist.Count > (System.Processes.Count / 2)) {
                            int maxts, rr;
                            (maxts, rr, readval) = NNAtomicRegister.Highest(readlist.Values);
                            readlist.Clear();

                            Action<NnarInternalWrite> internalWriteBuilder;
                            if (reading) {
                                internalWriteBuilder = (self) => {
                                    self.ReadId = readid;
                                    self.Timestamp = maxts;
                                    self.WriterRank = rr;
                                    self.Value = readval;
                                };
                            } else {
                                internalWriteBuilder = (self) => {
                                    self.ReadId = readid;
                                    self.Timestamp = maxts + 1;
                                    self.WriterRank = System.CurrentProcess.Rank;
                                    self.Value = writeval;
                                };
                            }

                            System.EventQueue.RegisterMessage(
                                BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                                    self.Message = BuildMessage<NnarInternalWrite>(AbstractionId, internalWriteBuilder);
                                })
                            );
                        }

                        break;
                    }

                    case Message.Types.Type.NnarInternalAck:  {
                        if (readid != innerMessage.NnarInternalAck.ReadId) return false;

                        acks += 1;
                        if (acks > (System.Processes.Count / 2)) {
                            acks = 0;

                            Message m;
                            if (reading) {
                                reading = false;

                                m = BuildMessage<NnarReadReturn>(ToParentAbstraction(), (self) => {
                                    self.Value = readval;
                                }, (outer) => { outer.FromAbstractionId = AbstractionId; });
                            } else {
                                m = BuildMessage<NnarWriteReturn>(ToParentAbstraction(), (_) => {}, (outer) => { outer.FromAbstractionId = AbstractionId; });
                            }

                            System.EventQueue.RegisterMessage(m);
                        }

                        break;
                    }

                    default:
                        throw new Exception($"Cannot handle message of type {innerMessage.Type}");
                }

                return true;
            });
        }

        private static Tuple<int, int, Value> Highest(IEnumerable<Tuple<int, int, Value>> values)
        {
            if (values.Count() == 0) return Tuple.Create<int, int, Value>(-1, -1, null);

            var highest = values.First();
            foreach (var value in values) {
                if (value.Item1 > highest.Item1 ||
                    (value.Item1 == highest.Item1 && value.Item2 > highest.Item2)) {
                    highest = value;
                }
            }

            return highest;
        }
    }
}