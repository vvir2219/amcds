using System;

namespace Project
{
    class ConditionEvent : Event
    {
        public Func<bool> Condition { get; private set; }
        public Action Action { get; private set; }

        public ConditionEvent(Func<bool> condition, Action action) : base(EventType.CONDITION)
        {
            Condition = condition;
            Action = action;
        }

        public override bool HasCondtitionSatisfied()
        {
            return Condition();
        }

        public override void Execute()
        {
            Action();
        }
    }
}