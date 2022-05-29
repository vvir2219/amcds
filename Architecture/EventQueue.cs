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
            var algorithm = System.GetAlgorithm(message.ToAbstractionId);
            if (algorithm == null) {
                algorithm = System.RegisterAlgorithmStack(message.ToAbstractionId);
            }
            algorithm.RegisterMessage(message);
        }
    }
}