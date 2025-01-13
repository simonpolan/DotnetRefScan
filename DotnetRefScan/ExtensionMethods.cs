using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static ICollection<T> LatestAndSorted<T>(this ICollection<T> references)
            where T : PackageReference
        {
            int maxDigits = references.GetMaxVersionDigitsCount();

            return references
                .GroupBy(r => $"{r.Source}-{r.Name}")
                .Select(r => r.OrderByDescending(v => v.GetVersionNumber(maxDigits)).First())
                .OrderBy(r => r.Source)
                .ThenBy(r => r.Name)
                .ToList();
        }

        public static long GetVersionNumber(this PackageReference packageReference, int expandToDigits)
        {
            string[] parts = packageReference.Version.Split('.');
            StringBuilder versionString = new StringBuilder();
            foreach (string part in parts)
            {
                if (long.TryParse(part, out long number))
                {
                    versionString.Append(number.ToString($"D{expandToDigits}"));
                }
            }
            string version = versionString.ToString();
            return string.IsNullOrEmpty(version) ? 0 : long.Parse(version);
        }

        public static int GetMaxVersionDigitsCount<T>(this ICollection<T> references)
            where T : PackageReference
        {
            return references
                .SelectMany(r => r.Version.Split('.'))
                .Distinct()
                .Where(part => long.TryParse(part, out long _))
                .Select(part => part.Length)
                .Max();
        }

        public static Func<T, bool> Not<T>(this Func<T, bool> predicate)
        {
            return value => !predicate(value);
        }
    }
}
