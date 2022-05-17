namespace Project
{
    class SystemState
    {
        public SystemInfo SystemInfo { get; set; }
        public NetworkHelper NetworkHelper { get; set; }

        public EventQueue eventQueue { get; set; }

    }
}