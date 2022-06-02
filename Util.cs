using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Protocol;

namespace Project
{
    class ListComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> obj)
        {
            int hashcode = 0;
            foreach (T t in obj)
            {
                hashcode ^= t.GetHashCode();
            }
            return hashcode;
        }
    }

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

        public static Tuple<List<object>, List<object>> DeconstructMessage(Message message, int depth = 1)
        {
            List<object> innerMessages = new List<object>(),
                         outerMessages = new List<object>();

            object innerMessage;
            var outerMessage = message;
            while(depth > 0)
            {
                innerMessage = outerMessage.GetInnerMessageByOwnType();

                outerMessages.Add(outerMessage);
                innerMessages.Add(innerMessage);

                if (depth > 1)
                {
                    if (! Util.HasProperty<Message>(innerMessage))
                        throw new Exception("Trying to deconstruct message that's not deep enough");

                    outerMessage = Util.GetProperty<Message>(innerMessage);
                }

                depth--;
            }

            return Tuple.Create(innerMessages, outerMessages);
        }

        public static Message BuildMessage<T>(string systemId, string fromAbstractionId, string toAbstractionId, Action<T> innerBuilder = null, Action<Message> outerBuilder = null) where T : new()
        {
            T innerMessage = new T();
            if (innerBuilder != null) innerBuilder(innerMessage);

            var message = new Message{
                FromAbstractionId = fromAbstractionId,
                ToAbstractionId = toAbstractionId
            };
            if (systemId != null)
                message.SystemId = systemId;

            message.Type = MessageType<T>();
            message.SetInnerMessage<T>(innerMessage);

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

        public static T GetInnerMessage<T>(this Message message)
        {
            return (T)message.GetType().GetProperty(typeof(T).Name).GetValue(message);
        }

        public static object GetInnerMessageByOwnType(this Message message)
        {
            var name = Enum.GetName(typeof(Message.Types.Type), message.Type);
            return message.GetType().GetProperty(name).GetValue(message);
        }

        public static void SetInnerMessage<T>(this Message message, T innerMessage)
        {
            message.GetType().GetProperty(typeof(T).Name).SetValue(message, innerMessage);
        }

        public static Message.Types.Type MessageType<T>()
        {
            return (Message.Types.Type)Enum.Parse(typeof(Message.Types.Type), typeof(T).Name);
        }

        public static bool HasProperty<T>(object obj)
        {
            return HasProperty(obj, typeof(T).Name);
        }
        public static bool HasProperty(object obj, string propName)
        {
            return obj.GetType().GetProperty(propName) != null;
        }

        public static T GetProperty<T>(object obj)
        {
            return (T)GetProperty(obj, typeof(T).Name);
        }
        public static object GetProperty(object obj, string propName)
        {
            return obj.GetType().GetProperty(propName).GetValue(obj);
        }
    }
}