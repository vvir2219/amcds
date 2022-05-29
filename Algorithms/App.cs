using Protocol;

namespace Project
{
    class App : Algorithm
    {
        public static string InstanceName = "app";

        public App(System system, string instanceId, string abstractionId) : base(system, instanceId, abstractionId)
        {
            UponMessage(Message.Types.Type.AppBroadcast, (message) => {
                return true;
            });
        }
    }
}