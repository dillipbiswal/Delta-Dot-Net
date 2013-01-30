
using System.Collections.Generic;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public static class Extensions
    {
        public static T GetOrCreateValue<T, TK>(this IDictionary<TK, T> self, TK key)
            where T : new()
        {
            T value;
            if (self.TryGetValue(key, out value) == false)
            {
                value = new T();
                self.Add(key, value);
            }
            return value;
        }

        public static T GetValueOrDefault<T, TK>(this IDictionary<TK, T> self, TK key)
        {
            T value;
            if (self.TryGetValue(key, out value) == false)
                return default(T);
            return value;
        }
    }
}