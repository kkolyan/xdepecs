using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SafeEcs
{
    internal static class LangUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ShallowListToString<T>(this IEnumerable<T> list, string delimiter = ", ")
        {
            return list
                .Select(x => x.ToString())
                .Aggregate((a, b) => a + delimiter + b);
        }
    }
}