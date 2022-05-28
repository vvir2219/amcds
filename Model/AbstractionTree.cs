using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Project
{

    class AbstractionTree
    {
        // TODO make this shit thread safe
        private Algorithm algorithm = null;
        public Algorithm Algorithm {get {return algorithm; }}

        private Dictionary<string, AbstractionTree> subtree = new Dictionary<string, AbstractionTree>();

        public AbstractionTree() {}
        private AbstractionTree(Algorithm algorithm)
        {
            this.algorithm = algorithm;
        }

        public AbstractionTree this[string abstractionId]
        {
            get => this[Util.DeconstructToInstanceIds(abstractionId)];
        }
        public AbstractionTree this[List<string> instanceIds]
        {
            get => GetTreeAt(instanceIds);
        }

        public AbstractionTree GetTreeAt(List<string> instanceIds)
        {
            var tree = this;
            foreach(var instanceId in instanceIds) {
                if (! this.subtree.ContainsKey(instanceId)) return null;

                tree = tree.subtree[instanceId];
            }

            return this;
        }

        public bool ContainsKey(string instanceId)
        {
            return subtree.ContainsKey(instanceId);
        }

        public Algorithm GetAlgorithm(string abstractionId)
        {
            return this[abstractionId]?.Algorithm;
        }
        public Algorithm GetAlgorithm(List<string> instanceIds)
        {
            return this[instanceIds]?.Algorithm;
        }

        public AbstractionTree AddAlgorithm(string instanceId, Algorithm algorithm)
        {
            var tree = new AbstractionTree(algorithm);
            subtree[instanceId] = tree;
            return tree;
        }
    }
}