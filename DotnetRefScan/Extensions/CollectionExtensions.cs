using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotnetRefScan.Extensions
{
    /// <summary>
    /// Extension methods for collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Gets distinct values by the specified key.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TKey">Key selector type.</typeparam>
        /// <param name="source">Source.</param>
        /// <param name="keySelector">Key selector.</param>
        /// <param name="comparer">Key comparer (optional).</param>
        /// <returns>Distinct collection.</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
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

        /// <summary>
        /// Gets distinct and sorted references.
        /// </summary>
        /// <typeparam name="T"><see cref="PackageReference"/></typeparam>
        /// <param name="references">References.</param>
        /// <returns>Distinct and sorted references</returns>
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


        /// <summary>
        /// Gets latest and sorted references.
        /// </summary>
        /// <typeparam name="T"><see cref="PackageReference"/></typeparam>
        /// <param name="references">References.</param>
        /// <returns>Latest and sorted references</returns>
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

        /// <summary>
        /// Tries get an item from the list by its index.
        /// </summary>
        /// <param name="list">List to search at.</param>
        /// <param name="index">Item index.</param>
        /// <returns>Item or <c>null</c>.</returns>
        public static string? TryGet(this IList<string> list, int? index)
        {
            return index.HasValue && list.Count > index.Value ? list[index.Value].Trim() : null;
        }

        private static long GetVersionNumber(this PackageReference packageReference, int expandToDigits)
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

        private static int GetMaxVersionDigitsCount<T>(this ICollection<T> references)
            where T : PackageReference
        {
            return references
                .SelectMany(r => r.Version.Split('.'))
                .Distinct()
                .Where(part => long.TryParse(part, out long _))
                .Select(part => part.Length)
                .Max();
        }
    }
}
