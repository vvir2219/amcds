using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace Project
{
    static class Util
    {
        public static List<string> DeconstructToInstanceIds(string abstractionId)
        {
            return abstractionId.Split(".").ToList();
        }

        // ex nnar[topic] -> (nnar, topic)
        //    beb -> (beb, "")
        public static Tuple<string, string> DeconstructToInstanceNameAndIndex(string instanceId)
        {
            var pattern = @"([^\[]*)(\[([^\]]*)\])?";
            var match = Regex.Match(instanceId, pattern);
            return Tuple.Create(match.Groups[1].Value, match.Groups[3].Value);
        }
    }
}