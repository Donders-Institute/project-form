using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dccn.ProjectForm.Extensions
{
    public static class EnumerableExtensions
    {
        public static async Task<List<TSource>> ToListAsync<TSource>(this IEnumerable<Task<TSource>> enumerable)
        {
            return (await Task.WhenAll(enumerable)).ToList();
        }
    }
}