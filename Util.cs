using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Protocol;

namespace Project
{
    static class Util
    {
        public static List<string> DeconstructToInstanceIds(string abstractionId)
        {
            if (abstractionId == null || abstractionId == string.Empty)
                return Array.Empty<string>().ToList();

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

        private static Dictionary<Type, Action<Message, object>> @switch = new Dictionary<Type, Action<Message, object>>() {
            { typeof(NetworkMessage), (message, innerMessage) => { message.Type = Message.Types.Type.NetworkMessage; message.NetworkMessage = innerMessage as NetworkMessage; } },
            { typeof(ProcRegistration), (message, innerMessage) => { message.Type = Message.Types.Type.ProcRegistration; message.ProcRegistration = innerMessage as ProcRegistration; } },
            { typeof(ProcInitializeSystem), (message, innerMessage) => { message.Type = Message.Types.Type.ProcInitializeSystem; message.ProcInitializeSystem = innerMessage as ProcInitializeSystem; } },
            { typeof(ProcDestroySystem), (message, innerMessage) => { message.Type = Message.Types.Type.ProcDestroySystem; message.ProcDestroySystem = innerMessage as ProcDestroySystem; } },
            { typeof(AppBroadcast), (message, innerMessage) => { message.Type = Message.Types.Type.AppBroadcast; message.AppBroadcast = innerMessage as AppBroadcast; } },
            { typeof(AppValue), (message, innerMessage) => { message.Type = Message.Types.Type.AppValue; message.AppValue = innerMessage as AppValue; } },
            { typeof(AppDecide), (message, innerMessage) => { message.Type = Message.Types.Type.AppDecide; message.AppDecide = innerMessage as AppDecide; } },
            { typeof(AppPropose), (message, innerMessage) => { message.Type = Message.Types.Type.AppPropose; message.AppPropose = innerMessage as AppPropose; } },
            { typeof(AppRead), (message, innerMessage) => { message.Type = Message.Types.Type.AppRead; message.AppRead = innerMessage as AppRead; } },
            { typeof(AppWrite), (message, innerMessage) => { message.Type = Message.Types.Type.AppWrite; message.AppWrite = innerMessage as AppWrite; } },
            { typeof(AppReadReturn), (message, innerMessage) => { message.Type = Message.Types.Type.AppReadReturn; message.AppReadReturn = innerMessage as AppReadReturn; } },
            { typeof(AppWriteReturn), (message, innerMessage) => { message.Type = Message.Types.Type.AppWriteReturn; message.AppWriteReturn = innerMessage as AppWriteReturn; } },
            { typeof(UcDecide), (message, innerMessage) => { message.Type = Message.Types.Type.UcDecide; message.UcDecide = innerMessage as UcDecide; } },
            { typeof(UcPropose), (message, innerMessage) => { message.Type = Message.Types.Type.UcPropose; message.UcPropose = innerMessage as UcPropose; } },
            { typeof(EpAbort), (message, innerMessage) => { message.Type = Message.Types.Type.EpAbort; message.EpAbort = innerMessage as EpAbort; } },
            { typeof(EpAborted), (message, innerMessage) => { message.Type = Message.Types.Type.EpAborted; message.EpAborted = innerMessage as EpAborted; } },
            { typeof(EpDecide), (message, innerMessage) => { message.Type = Message.Types.Type.EpDecide; message.EpDecide = innerMessage as EpDecide; } },
            { typeof(EpInternalAccept), (message, innerMessage) => { message.Type = Message.Types.Type.EpInternalAccept; message.EpInternalAccept = innerMessage as EpInternalAccept; } },
            { typeof(EpInternalDecided), (message, innerMessage) => { message.Type = Message.Types.Type.EpInternalDecided; message.EpInternalDecided = innerMessage as EpInternalDecided; } },
            { typeof(EpInternalRead), (message, innerMessage) => { message.Type = Message.Types.Type.EpInternalRead; message.EpInternalRead = innerMessage as EpInternalRead; } },
            { typeof(EpInternalState), (message, innerMessage) => { message.Type = Message.Types.Type.EpInternalState; message.EpInternalState = innerMessage as EpInternalState; } },
            { typeof(EpInternalWrite), (message, innerMessage) => { message.Type = Message.Types.Type.EpInternalWrite; message.EpInternalWrite = innerMessage as EpInternalWrite; } },
            { typeof(EpPropose), (message, innerMessage) => { message.Type = Message.Types.Type.EpPropose; message.EpPropose = innerMessage as EpPropose; } },
            { typeof(EcInternalNack), (message, innerMessage) => { message.Type = Message.Types.Type.EcInternalNack; message.EcInternalNack = innerMessage as EcInternalNack; } },
            { typeof(EcInternalNewEpoch), (message, innerMessage) => { message.Type = Message.Types.Type.EcInternalNewEpoch; message.EcInternalNewEpoch = innerMessage as EcInternalNewEpoch; } },
            { typeof(EcStartEpoch), (message, innerMessage) => { message.Type = Message.Types.Type.EcStartEpoch; message.EcStartEpoch = innerMessage as EcStartEpoch; } },
            { typeof(BebBroadcast), (message, innerMessage) => { message.Type = Message.Types.Type.BebBroadcast; message.BebBroadcast = innerMessage as BebBroadcast; } },
            { typeof(BebDeliver), (message, innerMessage) => { message.Type = Message.Types.Type.BebDeliver; message.BebDeliver = innerMessage as BebDeliver; } },
            { typeof(EldTimeout), (message, innerMessage) => { message.Type = Message.Types.Type.EldTimeout; message.EldTimeout = innerMessage as EldTimeout; } },
            { typeof(EldTrust), (message, innerMessage) => { message.Type = Message.Types.Type.EldTrust; message.EldTrust = innerMessage as EldTrust; } },
            { typeof(NnarInternalAck), (message, innerMessage) => { message.Type = Message.Types.Type.NnarInternalAck; message.NnarInternalAck = innerMessage as NnarInternalAck; } },
            { typeof(NnarInternalRead), (message, innerMessage) => { message.Type = Message.Types.Type.NnarInternalRead; message.NnarInternalRead = innerMessage as NnarInternalRead; } },
            { typeof(NnarInternalValue), (message, innerMessage) => { message.Type = Message.Types.Type.NnarInternalValue; message.NnarInternalValue = innerMessage as NnarInternalValue; } },
            { typeof(NnarInternalWrite), (message, innerMessage) => { message.Type = Message.Types.Type.NnarInternalWrite; message.NnarInternalWrite = innerMessage as NnarInternalWrite; } },
            { typeof(NnarRead), (message, innerMessage) => { message.Type = Message.Types.Type.NnarRead; message.NnarRead = innerMessage as NnarRead; } },
            { typeof(NnarReadReturn), (message, innerMessage) => { message.Type = Message.Types.Type.NnarReadReturn; message.NnarReadReturn = innerMessage as NnarReadReturn; } },
            { typeof(NnarWrite), (message, innerMessage) => { message.Type = Message.Types.Type.NnarWrite; message.NnarWrite = innerMessage as NnarWrite; } },
            { typeof(NnarWriteReturn), (message, innerMessage) => { message.Type = Message.Types.Type.NnarWriteReturn; message.NnarWriteReturn = innerMessage as NnarWriteReturn; } },
            { typeof(EpfdInternalHeartbeatReply), (message, innerMessage) => { message.Type = Message.Types.Type.EpfdInternalHeartbeatReply; message.EpfdInternalHeartbeatReply = innerMessage as EpfdInternalHeartbeatReply; } },
            { typeof(EpfdInternalHeartbeatRequest), (message, innerMessage) => { message.Type = Message.Types.Type.EpfdInternalHeartbeatRequest; message.EpfdInternalHeartbeatRequest = innerMessage as EpfdInternalHeartbeatRequest; } },
            { typeof(EpfdSuspect), (message, innerMessage) => { message.Type = Message.Types.Type.EpfdSuspect; message.EpfdSuspect = innerMessage as EpfdSuspect; } },
            { typeof(EpfdTimeout), (message, innerMessage) => { message.Type = Message.Types.Type.EpfdTimeout; message.EpfdTimeout = innerMessage as EpfdTimeout; } },
            { typeof(PlDeliver), (message, innerMessage) => { message.Type = Message.Types.Type.PlDeliver; message.PlDeliver = innerMessage as PlDeliver; } },
            { typeof(PlSend), (message, innerMessage) => { message.Type = Message.Types.Type.PlSend; message.PlSend = innerMessage as PlSend; } }
        };

        public static Message BuildMessage<T>(string fromAbstractionId, string toAbstractionId, Action<T> innerBuilder = null, Action<Message> outerBuilder = null) where T : new()
        {
            T innerMessage = new T();
            if (innerBuilder != null) innerBuilder(innerMessage);

            var message = new Message{
                FromAbstractionId = fromAbstractionId,
                ToAbstractionId = toAbstractionId
            };

            @switch[typeof(T)](message, innerMessage);

            if (outerBuilder != null) outerBuilder(message);
            return message;
        }

        public static ProcessId Maxrank(IEnumerable<ProcessId> processes)
        {
            if (processes.Count() == 0)
                throw new Exception("Cannot compute maxrank with no processes");

            var max = processes.First();
            foreach (var process in processes) {
                if (process.Rank > max.Rank)
                    max = process;
            }
            return max;
        }
    }
}