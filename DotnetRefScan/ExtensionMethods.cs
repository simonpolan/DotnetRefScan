using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetRefScan
{
    internal static class ExtensionMethods
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => DistinctBy(source, keySelector, null);

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var set = new HashSet<TKey>(comparer);
                do
                {
                    TSource element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
                while (enumerator.MoveNext());
            }
        }

        public static ICollection<T> DistinctAndSorted<T>(this ICollection<T> references)
            where T : PackageReference
        {
            return references
                .DistinctBy(r => $"{r.Source}-{r.Name}-{r.Version}")
                .OrderBy(r => r.Source)
                .ThenBy(r => r.Name)
                .ThenBy(r => r.Version)
                .ToList();
        }

        public static Func<T, bool> Not<T>(this Func<T, bool> predicate)
        {
            return value => !predicate(value);
        }
    }
}
