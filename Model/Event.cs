namespace Project
{
    enum EventType
    {
        MESSAGE,
        CONDITION,
        ACTION // one time action
    }

    abstract class Event
    {
        public EventType Type { get; private set; }

        public Event(EventType type)
        {
            Type = type;
        }

        public abstract bool HasCondtitionSatisfied();

        public abstract void Execute();
    }
}