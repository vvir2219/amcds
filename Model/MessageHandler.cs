using System;
using System.Collections.Generic;
using Protocol;

namespace Project
{
    class MessageHandler
    {
        public int Depth { get; private set; }
        public Func<List<object>, List<object>, bool> Condition { get; private set; }
        public Action<List<object>, List<object>> Action { get; private set; }

        public MessageHandler(int depth, Action<List<object>, List<object>> action)
        {
            Depth = depth;
            Action = action;
            Condition = (_, __) => true;
        }

        public MessageHandler(
            int depth,
            Func<List<object>, List<object>, bool> condition,
            Action<List<object>, List<object>> action)
        {
            Depth = depth;
            Condition = condition;
            Action = action;
        }
    }
}