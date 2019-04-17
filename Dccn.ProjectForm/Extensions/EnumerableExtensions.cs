using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Dccn.ProjectForm.Extensions
{
    public static class EnumerableExtensions
    {
        public static async Task<List<TSource>> ToListAsync<TSource>(this IEnumerable<Task<TSource>> source)
        {
            return (await Task.WhenAll(source)).ToList();
        }

        public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this IEnumerable<Task<TSource>> source, Func<TSource, TKey> keySelector)
        {
            return (await Task.WhenAll(source)).ToDictionary(keySelector);
        }

        public static async Task<ImmutableSortedDictionary<TKey, TSource>> ToImmutableSortedDictionaryAsync<TSource, TKey>(
            this IEnumerable<Task<TSource>> source, Func<TSource, TKey> keySelector)
        {
            return (await Task.WhenAll(source)).ToImmutableSortedDictionary(keySelector, x => x);
        }

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}