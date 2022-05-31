using System;
using Protocol;

namespace Project
{
    enum EventType
    {
        MESSAGE,
        CONDITION,
        ACTION // one time action
    }

    class Event
    {
        public Func<Message, bool> Condition { get; private set; }
        public Message Message { get;  private set; }
        public Action<Message> Action { get; private set; }
        public EventType Type { get; private set; }

        private Event(Func<Message, bool> condition, Message message, Action<Message> action, EventType type)
        {
            Condition = condition;
            Message = message;
            Action = action;
            Type = type;
        }

        public static Event NewMessageEvent(Func<Message, bool> condition, Message message, Action<Message> action)
        {
            return new Event(condition, message, action, EventType.MESSAGE);
        }
        public static Event NewMessageEvent(Message message, Action<Message> action)
        {
            return new Event((_) => true, message, action, EventType.MESSAGE);
        }
        public static Event NewConditionEvent(Func<bool> condition, Action action)
        {
            return new Event((_) => condition(), null, (_) => { action(); }, EventType.CONDITION);
        }
        public static Event NewActionEvent(Action action)
        {
            return new Event((_) => true, null, (_) => { action(); }, EventType.ACTION);
        }

        public bool HasCondtitionSatisfied()
        {
            return Condition(Message);
        }

        public void Execute()
        {
            Action(Message);
        }
    }
}