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

        public List<ProcessId> processes = new List<ProcessId>();
        public List<ProcessId> Processes { get { return processes; } set { processes = value; SetCurrentProcess(); } }

        public ProcessId CurrentProcess { get; private set;}
        public EventQueue EventQueue { get; private set;}

        private AbstractionTree Algorithms = new AbstractionTree();

        public System(SystemInfo systemInfo)
        {
            SystemInfo = systemInfo;
        }

        public Algorithm GetAlgorithm(string abstractionId) { return Algorithms.GetAlgorithm(abstractionId); }
        public void RegisterAlgorithmStack(string abstractionId)
        {
            RegisterAlgorithmStack(Util.DeconstructToInstanceIds(abstractionId));
        }
        public void RegisterAlgorithmStack(List<string> instanceIds)
        {
            if (instanceIds.Count == 0) return;

            AbstractionTree tree = Algorithms;
            foreach (var instanceId in instanceIds) {
                if (! tree.ContainsKey(instanceId))
                    tree.AddAlgorithm(instanceId, CreateAlgorithm(instanceId,
                                                                  (tree.Algorithm?.AbstractionId ?? "") + instanceId));
                tree = tree[instanceId];
            }
        }

        private Algorithm CreateAlgorithm(string instanceId, string abstractionId)
        {
            var (instanceName, instanceIndex) = Util.DeconstructToInstanceNameAndIndex(instanceId);
            switch (instanceName)
            {
                case "app": return new App(this, instanceId, abstractionId);

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
    }
}