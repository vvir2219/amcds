using System;
using Protocol;

namespace Project
{
    class MessageHandler
    {
        public Func<Message, bool> Condition {get; private set; }
        public Action<Message> Action {get; private set; }

        public MessageHandler(Action<Message> action)
        {
            Action = action;
            Condition = (_) => true;
        }

        public MessageHandler(Func<Message, bool> condition, Action<Message> action)
        {
            Action = action;
            Condition = condition;
        }
    }
}