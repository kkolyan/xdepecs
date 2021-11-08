using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeEcs
{
    internal class KahnTopologicalSorter<T>
    {
        public IList<T> GetSorted(IEnumerable<T> roots, IEnumerable<Edge<T>> edges)
        {
            var result = new List<T>();
            var originalRoots = roots.ToList();
            var currentRoots = new Queue<T>(originalRoots);

            ISet<Edge<T>> edgesSet;
            if (edges is ISet<Edge<T>> set)
            {
                edgesSet = set;
            }
            else
            {
                edgesSet = new HashSet<Edge<T>>(edges);
            }

            IDictionary<T, Queue<Edge<T>>> edgesByFrom = edgesSet
                .GroupBy(edge => edge.from)
                .ToDictionary(grouping => grouping.Key, grouping => new Queue<Edge<T>>(grouping));

            IDictionary<T, List<Edge<T>>> edgesByTo = edgesSet
                .GroupBy(edge => edge.to)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

            while (currentRoots.Count > 0)
            {
                var n = currentRoots.Dequeue();
                result.Add(n);

                var edgesFromN = edgesByFrom.GetOrDefault(n);
                if (edgesFromN != null)
                {
                    while (edgesFromN.Count > 0)
                    {
                        var e = edgesFromN.Dequeue();
                        var m = e.to;
                        var edgesToM = edgesByTo.GetOrDefault(m);
                        edgesToM?.Remove(e);
                        if (edgesToM == null || edgesToM.Count == 0)
                        {
                            edgesByTo.Remove(m);
                            currentRoots.Enqueue(m);
                        }
                    }

                    edgesByFrom.Remove(n);
                }
            }

            if (edgesByFrom.Count > 0 || edgesByTo.Count > 0)
            {
                throw new Exception("failed to sort topologically:" +
                                    $"\n    roots: {originalRoots.ShallowListToString()}" +
                                    $"\n    graph: {edgesSet.ShallowListToString()}" +
                                    $"\n    edgesByFrom: {edgesByFrom.SelectMany(x => x.Value).ShallowListToString()}" +
                                    $"\n    edgesByTo: {edgesByTo.SelectMany(x => x.Value).ShallowListToString()}");
            }

            return result;
        }
        
    }
    
    
    internal readonly struct Edge<T>
    {
        public readonly T from;
        public readonly T to;

        public Edge(T from, T to)
        {
            this.from = from;
            this.to = to;
        }

        public override string ToString()
        {
            return "{" + from + "=>" + to + "}";
        }

        private sealed class FromToEqualityComparer : IEqualityComparer<Edge<T>>
        {
            public bool Equals(Edge<T> x, Edge<T> y)
            {
                return EqualityComparer<T>.Default.Equals(x.from, y.from) &&
                       EqualityComparer<T>.Default.Equals(x.to, y.to);
            }

            public int GetHashCode(Edge<T> obj)
            {
                unchecked
                {
                    return (EqualityComparer<T>.Default.GetHashCode(obj.from) * 397) ^
                           EqualityComparer<T>.Default.GetHashCode(obj.to);
                }
            }
        }

        public static IEqualityComparer<Edge<T>> Comparer { get; } = new FromToEqualityComparer();
    }
}