using System.Collections.Generic;
using System.Linq;

namespace DifferenceComparer
{
    public static class EnumerableExtensionMethods
    {
        public static IEnumerable<List<T>> CustomChunk<T>(
            this IEnumerable<T> source,
            int? chunkSize)
        {
            if (!chunkSize.HasValue)
            {
                yield return source.ToList();
            }
            else
            {
                using var enumerator = source.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    yield return GetChunk(enumerator, chunkSize.Value).ToList();
                }
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


        public static IEnumerable<(TFirst? First, TSecond? Second)> CustomZip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
            where TFirst : class
            where TSecond : class
        {
            return ZipIterator(first, second);
        }

        private static IEnumerable<(TFirst? First, TSecond? Second)> ZipIterator<TFirst, TSecond>(
            IEnumerable<TFirst> first,
            IEnumerable<TSecond> second)
            where TFirst : class
            where TSecond : class
        {
            using IEnumerator<TFirst> e1 = first.GetEnumerator();
            using IEnumerator<TSecond> e2 = second.GetEnumerator();

            bool hasE1 = true;
            bool hasE2 = true;

            while (hasE1 || hasE2)
            {
                if (hasE1)
                {
                    hasE1 = e1.MoveNext();
                }

                if (hasE2)
                {
                    hasE2 = e2.MoveNext();
                }

                if (!hasE1 && !hasE2)
                {
                    yield break;
                }

                yield return (hasE1 ? e1.Current : null, hasE2 ? e2.Current : null);
            }
        }
    }
}
