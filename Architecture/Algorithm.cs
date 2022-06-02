using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private Dictionary<List<Message.Types.Type>, MessageHandler> messageHandlers = new Dictionary<List<Message.Types.Type>, MessageHandler>(new ListComparer<Message.Types.Type>());

        public Algorithm(System system, string instanceId, string abstractionId, Algorithm parent)
        {
            System = system;
            InstanceId = instanceId;
            AbstractionId = abstractionId;
            Parent = parent;
            StartHandlingEvents();
        }

        protected void RegisterAbstractionStack(string abstractionid)
        {
            (new Thread(() => { System.RegisterAbstractionStack(abstractionid); })).Start();
        }

        public void RegisterMessage(Message message)
        {
            // searching for handlers starting with the most specific to the most generic
            var typesList = new List<Message.Types.Type>();

            object innerMessage;
            var outerMessage = message;
            do {
                typesList.Add(outerMessage.Type);
                innerMessage = outerMessage.GetInnerMessageByOwnType();

                if (innerMessage != null && Util.HasProperty<Message>(innerMessage))
                    outerMessage = Util.GetProperty<Message>(innerMessage);
                else
                    break;
            } while (true);

            while (typesList.Count > 0)
            {
                if (messageHandlers.ContainsKey(typesList)) {
                    var handler = messageHandlers[typesList];
                    RegisterEvent(new MessageEvent(
                        handler.Depth,
                        handler.Condition,
                        message,
                        handler.Action
                    ));
                    return;
                }

                typesList.RemoveAt(typesList.Count -1);
            }

            throw new Exception($"Message could not be handled: {message}");
        }
        public void RegisterAction(Action action, int? delay = null)
        {
            RegisterEvent(new ActionEvent(action), delay);
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
                    lock(inactiveEvents) {
                        inactiveEvents.Enqueue(@event);
                    }
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

                        lock (inactiveEvents) {
                            while (anyEventHandled && inactiveEvents.Count > 0) {
                                var @event = inactiveEvents.Dequeue();
                                RegisterEvent(@event);
                            }
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

        // 1 message deep
        protected void UponMessage<T>(Action<T> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>() };
            messageHandlers[matcher] = new MessageHandler(
                1,
                (innerMessages, _) => {
                    action((T)innerMessages.First());
                });
        }
        protected void UponMessage<T>(Func<T, bool> condition, Action<T> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>() };
            messageHandlers[matcher] = new MessageHandler(
                1,
                (innerMessages, _) => condition((T)innerMessages.First()),
                (innerMessages, _) => {
                    action((T)innerMessages.First());
                });
        }
        protected void UponMessage<T>(Action<T, Message> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>() };
            messageHandlers[matcher] = new MessageHandler(
                1,
                (innerMessages, outerMessages) => {
                    action((T)innerMessages.First(), (Message)outerMessages.First());
                });
        }
        protected void UponMessage<T>(Func<T, Message, bool> condition, Action<T, Message> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>() };
            messageHandlers[matcher] = new MessageHandler(
                1,
                (innerMessages, outerMessages) =>
                    condition((T)innerMessages.First(), (Message)outerMessages.First()),
                (innerMessages, outerMessages) => {
                    action((T)innerMessages.First(), (Message)outerMessages.First());
                });
        }

        // 2 messages deep
        protected void UponMessage<T, T2>(Action<T, T2> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>(), Util.MessageType<T2>() };
            messageHandlers[matcher] = new MessageHandler(
                2,
                (innerMessages, _) => {
                    action((T)innerMessages.First(), (T2)innerMessages.ElementAt(1));
                });
        }
        protected void UponMessage<T, T2>(Func<T, T2, bool> condition, Action<T, T2> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>(), Util.MessageType<T2>() };
            messageHandlers[matcher] = new MessageHandler(
                2,
                (innerMessages, _) =>
                    condition((T)innerMessages.First(), (T2)innerMessages.ElementAt(1)),
                (innerMessages, _) => {
                    action((T)innerMessages.First(), (T2)innerMessages.ElementAt(1));
                });
        }
        protected void UponMessage<T, T2>(Action<T, T2, Message, Message> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>(), Util.MessageType<T2>() };
            messageHandlers[matcher] = new MessageHandler(
                2,
                (innerMessages, outerMessages) => {
                    action(
                        (T)innerMessages.First(),
                        (T2)innerMessages.ElementAt(1),
                        (Message)outerMessages.First(),
                        (Message)outerMessages.ElementAt(1));
                });
        }
        protected void UponMessage<T, T2>(Func<T, T2, Message, Message, bool> condition, Action<T, T2, Message, Message> action)
        {
            var matcher = new List<Message.Types.Type>{ Util.MessageType<T>(), Util.MessageType<T2>() };
            messageHandlers[matcher] = new MessageHandler(
                2,
                (innerMessages, outerMessages) =>
                    condition(
                        (T)innerMessages.First(),
                        (T2)innerMessages.ElementAt(1),
                        (Message)outerMessages.First(),
                        (Message)outerMessages.ElementAt(1)),
                (innerMessages, outerMessages) => {
                    action(
                        (T)innerMessages.First(),
                        (T2)innerMessages.ElementAt(1),
                        (Message)outerMessages.First(),
                        (Message)outerMessages.ElementAt(1));
                });
        }

        protected void UponCondition(Func<bool> condition, Action action)
        {
            RegisterEvent(new ConditionEvent(condition, action));
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
            return Util.BuildMessage<T>(System.SystemId, AbstractionId, toAbstractionId, innerBuilder, outerBuilder);
        }

        protected void Trigger(Message message)
        {
            if (Running) System.RegisterMessage(message);
        }
    }
}