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
            UponMessage<NnarRead>((_) => {
                readid += 1;
                acks = 0;
                readlist.Clear();
                reading = true;

                Trigger(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<NnarInternalRead>(AbstractionId, (self) => { self.ReadId = readid; });
                    })
                );
            });

            UponMessage<BebDeliver, NnarInternalRead>((bebDeliver, nnarInternalRead) => {
                Trigger(
                    BuildMessage<PlSend>(ToAbstraction("pl"), (self) => {
                        self.Destination = bebDeliver.Sender;
                        self.Message = BuildMessage<NnarInternalValue>(AbstractionId, (self) => {
                            self.ReadId = nnarInternalRead.ReadId;
                            self.Timestamp = timestamp;
                            self.WriterRank = writerRank;
                            self.Value = value;
                        });
                    })
                );
            });

            UponMessage<PlDeliver, NnarInternalValue>(
                (plDeliver, nnarInternalValue) => nnarInternalValue.ReadId == readid,
                (plDeliver, nnarInternalValue) => {
                    readlist[plDeliver.Sender] = Tuple.Create(
                            nnarInternalValue.Timestamp,
                            nnarInternalValue.WriterRank,
                            nnarInternalValue.Value
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

                        Trigger(
                            BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                                self.Message = BuildMessage<NnarInternalWrite>(AbstractionId, internalWriteBuilder);
                            })
                        );
                    }
                });

            UponMessage<NnarWrite>((nnarWrite) => {
                readid += 1;
                writeval = nnarWrite.Value;
                acks = 0;
                readlist.Clear();

                Trigger(
                    BuildMessage<BebBroadcast>(ToAbstraction("beb"), (self) => {
                        self.Message = BuildMessage<NnarInternalRead>(AbstractionId, (self) => { self.ReadId = readid; });
                    })
                );
            });

            UponMessage<BebDeliver, NnarInternalWrite>((bebDeliver, nnarInternalWrite) => {
                if (nnarInternalWrite.Timestamp > timestamp ||
                    (nnarInternalWrite.Timestamp == timestamp && nnarInternalWrite.WriterRank > writerRank)) {
                    timestamp = nnarInternalWrite.Timestamp;
                    writerRank = nnarInternalWrite.WriterRank;
                    value = nnarInternalWrite.Value;
                }

                Trigger(
                    BuildMessage<PlSend>(ToAbstraction("pl"), (self) =>{
                        self.Destination = bebDeliver.Sender;
                        self.Message = BuildMessage<NnarInternalAck>(AbstractionId, (self) => {
                            self.ReadId = nnarInternalWrite.ReadId;
                        });
                    })
                );
            });

            UponMessage<PlDeliver, NnarInternalAck>((plDeliver, nnarInternalAck) => {
                acks += 1;
                if (acks > (System.Processes.Count / 2)) {
                    acks = 0;

                    Message m;
                    if (reading) {
                        reading = false;
                        m = BuildMessage<NnarReadReturn>(ToParentAbstraction(), (self) => { self.Value = readval; });
                    } else {
                        m = BuildMessage<NnarWriteReturn>(ToParentAbstraction(), (_) => {});
                    }

                    Trigger(m);
                }
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