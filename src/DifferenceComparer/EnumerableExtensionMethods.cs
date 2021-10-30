using System.Collections.Generic;
using System.Linq;

namespace DifferenceComparer
{
    public static class EnumerableExtensionMethods
    {
        public static IEnumerable<List<T>> Chunk<T>(
            this IEnumerable<T> source,
            int chunkSize)
        {
            using var enumerator = source.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return GetChunk(enumerator, chunkSize).ToList();
            }
        }

        private static IEnumerable<T> GetChunk<T>(
            IEnumerator<T> enumerator,
            int chunkSize)
        {
            do
            {
                yield return enumerator.Current;
            } while (--chunkSize > 0 && enumerator.MoveNext());
        }
    }
}
