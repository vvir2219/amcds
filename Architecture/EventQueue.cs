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

        public void RegisterMessage(Message message, string toAbstractionId = null) {
            toAbstractionId = toAbstractionId ?? message.ToAbstractionId ?? "";

            var algorithm = System.GetAlgorithm(toAbstractionId);
            if (algorithm == null) {
                algorithm = System.RegisterAlgorithmStack(toAbstractionId);
            }
            algorithm.RegisterMessage(message);
        }
    }
}