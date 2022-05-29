using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Protocol;

namespace Project
{
    class System
    {
        public SystemInfo SystemInfo { get; set; }

        public string SystemId { get; set; }
        public List<ProcessId> processes = new List<ProcessId>();
        public List<ProcessId> Processes {
            get { lock(processes) { return processes; } }
            set { lock(processes) { processes.Clear(); processes.AddRange(value); SetCurrentProcess(); } }
        }

        public ProcessId CurrentProcess { get; private set;}
        public EventQueue EventQueue { get; private set;}
        public NetworkManager NetworkManager { get; set; }

        private AbstractionTree Algorithms;
        private readonly object algorithmsLock = new object();

        public System(SystemInfo systemInfo)
        {
            Algorithms = new AbstractionTree(new ProcManager(this, "", "", null));
            SystemInfo = systemInfo;
            EventQueue = new EventQueue(this);
        }

        public Algorithm GetAlgorithm(string abstractionId)
        {
            lock(algorithmsLock) {
                return Algorithms.GetAlgorithm(abstractionId);
            }
        }
        public Algorithm RegisterAlgorithmStack(string abstractionId)
        {
            return RegisterAlgorithmStack(Util.DeconstructToInstanceIds(abstractionId));
        }
        public Algorithm RegisterAlgorithmStack(List<string> instanceIds)
        {
            lock(algorithmsLock) {
                if (instanceIds.Count == 0) return null;

                AbstractionTree tree = Algorithms;
                foreach (var instanceId in instanceIds) {
                    if (! tree.ContainsKey(instanceId)) {
                        var lastAbstractionId = tree.Algorithm?.AbstractionId;
                        var abstractionId = (lastAbstractionId == null || lastAbstractionId == string.Empty ? "" : lastAbstractionId + ".") + instanceId;
                        tree.AddAlgorithm(instanceId, CreateAlgorithm(instanceId, abstractionId, tree.Algorithm));
                    }
                    tree = tree[instanceId];
                }
                return tree.Algorithm;
            }
        }

        private Algorithm CreateAlgorithm(string instanceId, string abstractionId, Algorithm parent)
        {
            var (instanceName, instanceIndex) = Util.DeconstructToInstanceNameAndIndex(instanceId);
            switch (instanceName)
            {
                case "app": return new App(this, instanceId, abstractionId, parent);
                case "pl" : return new PerfectLink(this, instanceId, abstractionId, parent);
                case "beb": return new BestEffortBroadcast(this, instanceId, abstractionId, parent);

                default:
                    throw new ArgumentException($"Could not register abstraction with id {instanceId}!");
            }
        }

        private void SetCurrentProcess()
        {
            foreach (var process in processes) {
                if (process.Owner == SystemInfo.SELF_OWNER &&
                    process.Index == SystemInfo.SELF_INDEX) {
                        CurrentProcess = process;
                        return;
                    }
            }
        }

        internal ProcessId GetProcessByHostAndPort(string senderHost, int senderListeningPort)
        {
            if (senderHost == SystemInfo.HUB_HOST && senderListeningPort == SystemInfo.HUB_PORT) return null;

            foreach (var process in Processes)
                if (process.Host == senderHost && process.Port == senderListeningPort)
                    return process;

            throw new ArgumentException($"Could not find process with host {senderHost} and port {senderListeningPort}");
        }
    }
}