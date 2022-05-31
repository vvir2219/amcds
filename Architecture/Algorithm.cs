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
        private Queue<Event> activeEvents = new Queue<Event>(),
                             inactiveEvents = new Queue<Event>();
        private Dictionary<Message.Types.Type, MessageHandler> messageHandlers = new Dictionary<Message.Types.Type, MessageHandler>();


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
            var handler = messageHandlers[message.Type];

            RegisterEvent(Event.NewMessageEvent(handler.Condition, message, handler.Action));
        }
        public void RegisterAction(Action action, int? delay = null)
        {
            RegisterEvent(Event.NewActionEvent(action), delay);
        }
        public void RegisterEvent(Event @event, int? delay = null)
        {
            (new Thread(() => {
                if (delay.HasValue) Thread.Sleep(delay.Value);

                if (@event.HasCondtitionSatisfied()) {
                    lock (activeEvents)
                    {
                        activeEvents.Enqueue(@event);
                        Monitor.Pulse(activeEvents);
                    }
                } else {
                    inactiveEvents.Enqueue(@event);
                }
            })).Start();
        }

        private void StartHandlingEvents()
        {
            Running = true;
            (new Thread(() => {
                while (Running) {
                    bool anyEventHandled = false;
                    // wait for a message/event to arrive
                    lock (activeEvents) {
                        while (activeEvents.Count > 0) {
                            // handle the message
                            var @event = activeEvents.Dequeue();
                            var eventHandled = HandleEvent(@event);
                            anyEventHandled = anyEventHandled || eventHandled;
                        };

                        while (anyEventHandled && inactiveEvents.Count > 0) {
                            var @event = inactiveEvents.Dequeue();
                            RegisterEvent(@event);
                        }

                        Monitor.Wait(activeEvents);
                    }
                }
            })).Start();
        }

        private bool HandleEvent(Event @event)
        {
            var eventHandled = false;

            switch (@event.Type) {
                case EventType.ACTION:
                    @event.Execute();
                    eventHandled = true;
                    break;

                case EventType.CONDITION:
                    if (! @event.HasCondtitionSatisfied()) {
                        eventHandled = false;
                    } else {
                        @event.Execute();
                        eventHandled = true;
                    }

                    RegisterEvent(@event, 50); // with a small delay in case condition is always true
                    break;

                case EventType.MESSAGE:
                    if (! @event.HasCondtitionSatisfied()) {
                        eventHandled = false;
                        RegisterEvent(@event);
                    } else {
                        @event.Execute();
                        eventHandled = true;
                    }
                    break;
            }

            return eventHandled;
        }

        protected void UponMessage(Message.Types.Type type, Action<Message> action)
        {
            messageHandlers[type] = new MessageHandler(action);
        }
        protected void UponMessage(Message.Types.Type type, Func<Message, bool> condition, Action<Message> action)
        {
            messageHandlers[type] = new MessageHandler(condition, action);
        }

        protected void UponCondition(Func<bool> condition, Action action)
        {
            RegisterEvent(Event.NewConditionEvent(condition, action));
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
            return Util.BuildMessage<T>(AbstractionId, toAbstractionId, innerBuilder, outerBuilder);
        }
    }
}