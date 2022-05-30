using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Google.Protobuf.Reflection;
using Protocol;

namespace Project
{
    abstract class Algorithm
    {
        public System System { get; set; }

        // own stuff
        // each subclass of Algorithm should have a static string representing their instance name
        // ex. beb, pl, uc, ep, app
        // we have 3 kinds of identifiers for algorithms: instance name, instance id, and abstraction id
        //   instance names: app, beb, pl, nnar, uc, ec, ep, eld, epfd
        //   instance ids: up[topic], nnar[register], ep[index]
        //   abstraction ids examples: app.pl, app.nnar[register].pl
        public string InstanceId { get; set; }
        public string AbstractionId { get; set; }
        public Algorithm Parent { get; private set; }

        public bool Running { get; set; }
        private Queue<Message> messagesToHandle = new Queue<Message>();
        private Queue<Action> eventsToHandle = new Queue<Action>();
        private Dictionary<Message.Types.Type, Func<Message, bool>> messageHandlers = new Dictionary<Message.Types.Type, Func<Message, bool>>();


        public Algorithm(System system, string instanceId, string abstractionId, Algorithm parent)
        {
            System = system;
            InstanceId = instanceId;
            AbstractionId = abstractionId;
            Parent = parent;
            StartHandlingEvents();
        }

        public void RegisterMessage(Message message)
        {
            (new Thread(() => {
                lock (messagesToHandle)
                {
                    messagesToHandle.Enqueue(message);
                    Monitor.Pulse(messagesToHandle);
                }
            })).Start();
        }

        protected void RegisterEvent(Action action, int delay)
        {
            (new Thread(() => {
                Thread.Sleep(delay);

                lock (messagesToHandle)
                {
                    eventsToHandle.Enqueue(action);
                    Monitor.Pulse(messagesToHandle);
                }
            })).Start();
        }

        private void StartHandlingEvents()
        {
            Running = true;
            (new Thread(() => {
                while (Running) {
                    // wait for a message/event to arrive
                    lock (messagesToHandle) {
                        while (messagesToHandle.Count > 0) {
                            // handle the message
                            var message = messagesToHandle.Dequeue();
                            HandleMessage(message);
                        };
                        while (eventsToHandle.Count > 0) {
                            eventsToHandle.Dequeue()();
                        };
                        Monitor.Wait(messagesToHandle);
                    }
                }
            })).Start();
        }

        private void HandleMessage(Message message)
        {
            if (! messageHandlers.ContainsKey(message.Type)) {
                throw new ArgumentException($"Message of type {message.Type} could not be handled");
            }

            var messageHandled = messageHandlers[message.Type](message);
            if (! messageHandled) {
                // put it in unhandled queue
            }
        }

        // private Dictionary<Message.Types.Type, Func<Message, bool>> messageHandlers;
        protected void UponMessage(Message.Types.Type type, Func<Message, bool> handler)
        {
            messageHandlers[type] = handler;
        }

        protected string ToAbstraction(string toInstanceId = "")
        {
            var toAbstractionId = AbstractionId + "." + toInstanceId;
            if (toInstanceId == null || toInstanceId == string.Empty) {
                toAbstractionId = Parent.AbstractionId;
            }
            return toAbstractionId;
        }

        protected string ToParentAbstraction()
        {
            return ToAbstraction();
        }

        // BuildMessage<BebBroadcast>(ToAbstraction("beb"), (message) => {
        //     message.Message = BuildMessage<AppValue>(AbstractionId, (message) => {
        //         message.Value = valueMessage;
        //     }));
        // });
        protected Message BuildMessage<T>(string toAbstractionId, Action<T> innerBuilder = null, Action<Message> outerBuilder = null) where T : new()
        {
            T innerMessage = new T();
            if (innerBuilder != null) innerBuilder(innerMessage);
            var message = new Message{
                ToAbstractionId = toAbstractionId
            };

            var @switch = new Dictionary<Type, Action>();
            @switch[typeof(NetworkMessage)] = () => {
                message.Type = Message.Types.Type.NetworkMessage;
                message.NetworkMessage = innerMessage as NetworkMessage;
            };
            @switch[typeof(ProcRegistration)] = () => {
                message.Type = Message.Types.Type.ProcRegistration;
                message.ProcRegistration = innerMessage as ProcRegistration;
            };
            @switch[typeof(ProcInitializeSystem)] = () => {
                message.Type = Message.Types.Type.ProcInitializeSystem;
                message.ProcInitializeSystem = innerMessage as ProcInitializeSystem;
            };
            @switch[typeof(ProcDestroySystem)] = () => {
                message.Type = Message.Types.Type.ProcDestroySystem;
                message.ProcDestroySystem = innerMessage as ProcDestroySystem;
            };
            @switch[typeof(AppBroadcast)] = () => {
                message.Type = Message.Types.Type.AppBroadcast;
                message.AppBroadcast = innerMessage as AppBroadcast;
            };
            @switch[typeof(AppValue)] = () => {
                message.Type = Message.Types.Type.AppValue;
                message.AppValue = innerMessage as AppValue;
            };
            @switch[typeof(AppDecide)] = () => {
                message.Type = Message.Types.Type.AppDecide;
                message.AppDecide = innerMessage as AppDecide;
            };
            @switch[typeof(AppPropose)] = () => {
                message.Type = Message.Types.Type.AppPropose;
                message.AppPropose = innerMessage as AppPropose;
            };
            @switch[typeof(AppRead)] = () => {
                message.Type = Message.Types.Type.AppRead;
                message.AppRead = innerMessage as AppRead;
            };
            @switch[typeof(AppWrite)] = () => {
                message.Type = Message.Types.Type.AppWrite;
                message.AppWrite = innerMessage as AppWrite;
            };
            @switch[typeof(AppReadReturn)] = () => {
                message.Type = Message.Types.Type.AppReadReturn;
                message.AppReadReturn = innerMessage as AppReadReturn;
            };
            @switch[typeof(AppWriteReturn)] = () => {
                message.Type = Message.Types.Type.AppWriteReturn;
                message.AppWriteReturn = innerMessage as AppWriteReturn;
            };
            @switch[typeof(UcDecide)] = () => {
                message.Type = Message.Types.Type.UcDecide;
                message.UcDecide = innerMessage as UcDecide;
            };
            @switch[typeof(UcPropose)] = () => {
                message.Type = Message.Types.Type.UcPropose;
                message.UcPropose = innerMessage as UcPropose;
            };
            @switch[typeof(EpAbort)] = () => {
                message.Type = Message.Types.Type.EpAbort;
                message.EpAbort = innerMessage as EpAbort;
            };
            @switch[typeof(EpAborted)] = () => {
                message.Type = Message.Types.Type.EpAborted;
                message.EpAborted = innerMessage as EpAborted;
            };
            @switch[typeof(EpDecide)] = () => {
                message.Type = Message.Types.Type.EpDecide;
                message.EpDecide = innerMessage as EpDecide;
            };
            @switch[typeof(EpInternalAccept)] = () => {
                message.Type = Message.Types.Type.EpInternalAccept;
                message.EpInternalAccept = innerMessage as EpInternalAccept;
            };
            @switch[typeof(EpInternalDecided)] = () => {
                message.Type = Message.Types.Type.EpInternalDecided;
                message.EpInternalDecided = innerMessage as EpInternalDecided;
            };
            @switch[typeof(EpInternalRead)] = () => {
                message.Type = Message.Types.Type.EpInternalRead;
                message.EpInternalRead = innerMessage as EpInternalRead;
            };
            @switch[typeof(EpInternalState)] = () => {
                message.Type = Message.Types.Type.EpInternalState;
                message.EpInternalState = innerMessage as EpInternalState;
            };
            @switch[typeof(EpInternalWrite)] = () => {
                message.Type = Message.Types.Type.EpInternalWrite;
                message.EpInternalWrite = innerMessage as EpInternalWrite;
            };
            @switch[typeof(EpPropose)] = () => {
                message.Type = Message.Types.Type.EpPropose;
                message.EpPropose = innerMessage as EpPropose;
            };
            @switch[typeof(EcInternalNack)] = () => {
                message.Type = Message.Types.Type.EcInternalNack;
                message.EcInternalNack = innerMessage as EcInternalNack;
            };
            @switch[typeof(EcInternalNewEpoch)] = () => {
                message.Type = Message.Types.Type.EcInternalNewEpoch;
                message.EcInternalNewEpoch = innerMessage as EcInternalNewEpoch;
            };
            @switch[typeof(EcStartEpoch)] = () => {
                message.Type = Message.Types.Type.EcStartEpoch;
                message.EcStartEpoch = innerMessage as EcStartEpoch;
            };
            @switch[typeof(BebBroadcast)] = () => {
                message.Type = Message.Types.Type.BebBroadcast;
                message.BebBroadcast = innerMessage as BebBroadcast;
            };
            @switch[typeof(BebDeliver)] = () => {
                message.Type = Message.Types.Type.BebDeliver;
                message.BebDeliver = innerMessage as BebDeliver;
            };
            @switch[typeof(EldTimeout)] = () => {
                message.Type = Message.Types.Type.EldTimeout;
                message.EldTimeout = innerMessage as EldTimeout;
            };
            @switch[typeof(EldTrust)] = () => {
                message.Type = Message.Types.Type.EldTrust;
                message.EldTrust = innerMessage as EldTrust;
            };
            @switch[typeof(NnarInternalAck)] = () => {
                message.Type = Message.Types.Type.NnarInternalAck;
                message.NnarInternalAck = innerMessage as NnarInternalAck;
            };
            @switch[typeof(NnarInternalRead)] = () => {
                message.Type = Message.Types.Type.NnarInternalRead;
                message.NnarInternalRead = innerMessage as NnarInternalRead;
            };
            @switch[typeof(NnarInternalValue)] = () => {
                message.Type = Message.Types.Type.NnarInternalValue;
                message.NnarInternalValue = innerMessage as NnarInternalValue;
            };
            @switch[typeof(NnarInternalWrite)] = () => {
                message.Type = Message.Types.Type.NnarInternalWrite;
                message.NnarInternalWrite = innerMessage as NnarInternalWrite;
            };
            @switch[typeof(NnarRead)] = () => {
                message.Type = Message.Types.Type.NnarRead;
                message.NnarRead = innerMessage as NnarRead;
            };
            @switch[typeof(NnarReadReturn)] = () => {
                message.Type = Message.Types.Type.NnarReadReturn;
                message.NnarReadReturn = innerMessage as NnarReadReturn;
            };
            @switch[typeof(NnarWrite)] = () => {
                message.Type = Message.Types.Type.NnarWrite;
                message.NnarWrite = innerMessage as NnarWrite;
            };
            @switch[typeof(NnarWriteReturn)] = () => {
                message.Type = Message.Types.Type.NnarWriteReturn;
                message.NnarWriteReturn = innerMessage as NnarWriteReturn;
            };
            @switch[typeof(EpfdInternalHeartbeatReply)] = () => {
                message.Type = Message.Types.Type.EpfdInternalHeartbeatReply;
                message.EpfdInternalHeartbeatReply = innerMessage as EpfdInternalHeartbeatReply;
            };
            @switch[typeof(EpfdInternalHeartbeatRequest)] = () => {
                message.Type = Message.Types.Type.EpfdInternalHeartbeatRequest;
                message.EpfdInternalHeartbeatRequest = innerMessage as EpfdInternalHeartbeatRequest;
            };
            @switch[typeof(EpfdSuspect)] = () => {
                message.Type = Message.Types.Type.EpfdSuspect;
                message.EpfdSuspect = innerMessage as EpfdSuspect;
            };
            @switch[typeof(EpfdTimeout)] = () => {
                message.Type = Message.Types.Type.EpfdTimeout;
                message.EpfdTimeout = innerMessage as EpfdTimeout;
            };
            @switch[typeof(PlDeliver)] = () => {
                message.Type = Message.Types.Type.PlDeliver;
                message.PlDeliver = innerMessage as PlDeliver;
            };
            @switch[typeof(PlSend)] = () => {
                message.Type = Message.Types.Type.PlSend;
                message.PlSend = innerMessage as PlSend;
            };

            @switch[typeof(T)]();

            if (outerBuilder != null) outerBuilder(message);
            return message;
        }
    }
}