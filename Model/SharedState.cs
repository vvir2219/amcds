using System.Collections.Generic;
using Protocol;

namespace Project
{
    class SharedState
    {
        public SystemInfo SystemInfo { get; set; }

        public List<ProcessId> processes = new List<ProcessId>();
        public List<ProcessId> Processes { get { return processes; } set { processes = value; } }

        public ProcessId CurrentProcess { get; private set;}

        private List<Algorithm> Algorithms = new List<Algorithm>();


        public void SetCurrentProcess()
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