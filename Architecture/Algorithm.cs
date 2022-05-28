namespace Project
{
    abstract class Algorithm
    {
        public System System { get; set; }

        // own stuff
        // each subclass of Algorithm should have a static string representing their instance name
        // ex. beb, pl, uc, ep, app
        // we have 3 kinds of identifiers for algorithms: instance name, instance id, and abstraction id
        //   instance names: app, beb, pl, nnar, uc, ec, ep, eld, epfd
        //   instance ids: up[topic], nnar[register], ep[index]
        //   abstraction ids examples: app.pl, app.nnar[register].pl
        public string InstanceId { get; set; }
        public string AbstractionId { get; set; }

        public Algorithm(System system, string instanceId, string abstractionId)
        {
            System = system;
            InstanceId = instanceId;
            AbstractionId = abstractionId;
        }

    }
}