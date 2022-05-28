using System;
using Protocol;

namespace Project
{
    class EventQueue
    {
        public System System { get; private set; }

        public EventQueue(System system) {
            System = system;
        }

        public void RegisterMessage(Message message) {
        }
    }
}