using System;

namespace Project
{
    class ActionEvent : Event
    {
        public Action Action { get; private set; }

        public ActionEvent(Action action) : base(EventType.ACTION)
        {
            Action = action;
        }

        public override bool HasCondtitionSatisfied()
        {
            return true;
        }

        public override void Execute()
        {
            Action();
        }
    }
}