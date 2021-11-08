using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SafeEcs
{
    internal static class LangUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TK, TV>(this Dictionary<TK, List<TV>> dict, TK key, TV value)
        {
            if (!dict.TryGetValue(key, out List<TV> list))
            {
                list = new List<TV>();
                dict[key] = list;
            }

            list.Add(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ShallowListToString<T>(this IEnumerable<T> list, string delimiter = ", ")
        {
            return list
                .Select(x => x.ToString())
                .Aggregate((a, b) => a + delimiter + b);
        }
    }
}