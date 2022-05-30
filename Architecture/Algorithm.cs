using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

        protected string ToAbstractionId(string toInstanceId = "")
        {
            var toAbstractionId = AbstractionId + "." + toInstanceId;
            if (toInstanceId == null || toInstanceId == string.Empty) {
                toAbstractionId = Parent.AbstractionId;
            }
            return toAbstractionId;
        }
    }
}