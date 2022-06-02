using System;
using System.Collections.Generic;
using Protocol;

namespace Project
{
    class MessageEvent : Event
    {
        public int Depth { get; private set; }
        public Func<List<object>, List<object>, bool> Condition { get; private set; }
        public Message Message { get;  private set; }
        public Action<List<object>, List<object>> Action { get; private set; }

        public MessageEvent(
            int depth,
            Func<List<object>, List<object>, bool> condition,
            Message message,
            Action<List<object>, List<object>> action) : base(EventType.MESSAGE)
        {
            Depth = depth;
            Condition = condition;
            Message = message;
            Action = action;
        }

        public override void Execute()
        {
            var (innerMessages, outerMessages) = Util.DeconstructMessage(Message, Depth);
            Action(innerMessages, outerMessages);
        }

        public override bool HasCondtitionSatisfied()
        {
            var (innerMessages, outerMessages) = Util.DeconstructMessage(Message, Depth);
            return Condition(innerMessages, outerMessages);
        }

    }
}