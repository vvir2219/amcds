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

                var internalRead = new Message {
                    Type = Message.Types.Type.NnarInternalRead,
                    ToAbstractionId = AbstractionId,
                    NnarInternalRead = new NnarInternalRead {
                        ReadId = readid
                    }
                };

                var bebBroadcast = new Message{
                    Type = Message.Types.Type.BebBroadcast,
                    ToAbstractionId = ToAbstractionId("beb"),
                    BebBroadcast = new BebBroadcast {
                        Message = internalRead
                    }
                };

                System.EventQueue.RegisterMessage(bebBroadcast);
                return true;
            });

            UponMessage(Message.Types.Type.NnarRead, (message) => {
                readid += 1;
                acks = 0;
                readlist.Clear();
                reading = true;

                var internalRead = new Message {
                    Type = Message.Types.Type.NnarInternalRead,
                    ToAbstractionId = AbstractionId,
                    NnarInternalRead = new NnarInternalRead {
                        ReadId = readid
                    }
                };

                var bebBroadcast = new Message{
                    Type = Message.Types.Type.BebBroadcast,
                    ToAbstractionId = ToAbstractionId("beb"),
                    BebBroadcast = new BebBroadcast {
                        Message = internalRead
                    }
                };

                System.EventQueue.RegisterMessage(bebBroadcast);
                return true;
            });

            UponMessage(Message.Types.Type.BebDeliver, (message) => {
                var innerMessage = message.BebDeliver.Message;
                switch (innerMessage.Type) {
                    case Message.Types.Type.NnarInternalRead: {
                        var internalValue = new Message {
                            Type = Message.Types.Type.NnarInternalValue,
                            ToAbstractionId = AbstractionId,
                            NnarInternalValue = new NnarInternalValue {
                                ReadId = innerMessage.NnarInternalRead.ReadId,
                                Timestamp = timestamp,
                                WriterRank = writerRank,
                                Value = value
                            }
                        };

                        var plSend = new Message {
                            Type = Message.Types.Type.PlSend,
                            ToAbstractionId = ToAbstractionId("pl"),
                            PlSend = new PlSend {
                                Destination = message.BebDeliver.Sender,
                                Message = internalValue
                            }
                        };

                        System.EventQueue.RegisterMessage(plSend);
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

                        var ack = new Message {
                            Type = Message.Types.Type.NnarInternalAck,
                            ToAbstractionId = AbstractionId,
                            NnarInternalAck = new NnarInternalAck {
                                ReadId = nnarInternalWrite.ReadId
                            }
                        };

                        var plSend = new Message {
                            Type = Message.Types.Type.PlSend,
                            ToAbstractionId = ToAbstractionId("pl"),
                            PlSend = new PlSend {
                                Destination = message.BebDeliver.Sender,
                                Message = ack
                            }
                        };

                        System.EventQueue.RegisterMessage(plSend);
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

                            Message internalWrite;
                            if (reading) {
                                internalWrite = new Message {
                                    Type = Message.Types.Type.NnarInternalWrite,
                                    ToAbstractionId = AbstractionId,
                                    NnarInternalWrite = new NnarInternalWrite{
                                        ReadId = readid,
                                        Timestamp = maxts,
                                        WriterRank = rr,
                                        Value = readval
                                    }
                                };
                            } else {
                                internalWrite = new Message
                                {
                                    Type = Message.Types.Type.NnarInternalWrite,
                                    ToAbstractionId = AbstractionId,
                                    NnarInternalWrite = new NnarInternalWrite
                                    {
                                        ReadId = readid,
                                        Timestamp = maxts + 1,
                                        WriterRank = System.CurrentProcess.Rank,
                                        Value = writeval
                                    }
                                };
                            }

                            var bebBroadcast = new Message{
                                Type = Message.Types.Type.BebBroadcast,
                                ToAbstractionId = ToAbstractionId("beb"),
                                BebBroadcast = new BebBroadcast {
                                    Message = internalWrite
                                }
                            };

                            System.EventQueue.RegisterMessage(bebBroadcast);
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
                                m = new Message {
                                    Type = Message.Types.Type.NnarReadReturn,
                                    FromAbstractionId = AbstractionId,
                                    ToAbstractionId = ToAbstractionId(),
                                    NnarReadReturn = new NnarReadReturn {
                                        Value = readval
                                    }
                                };
                            } else {
                                m = new Message {
                                    Type = Message.Types.Type.NnarWriteReturn,
                                    FromAbstractionId = AbstractionId,
                                    ToAbstractionId = ToAbstractionId(),
                                    NnarWriteReturn = new NnarWriteReturn()
                                };
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