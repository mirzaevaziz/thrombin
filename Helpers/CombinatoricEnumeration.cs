using System.Collections.Generic;
using System.Linq;

namespace thrombin.Helpers
{
    public static class CombinatoricEnumeration
    {
        // https://docs.python.org/2/library/itertools.html#itertools.combinations
        public static IEnumerable<List<T>> Combinations<T>(this IEnumerable<T> items, int r)
        {
            int n = items.Count();

            if (r > n) yield break;

            T[] pool = items.ToArray();
            int[] indices = Enumerable.Range(0, r).ToArray();

            yield return indices.Select(x => pool[x]).ToList();

            while (true)
            {
                int i = indices.Length - 1;
                while (i >= 0 && indices[i] == i + n - r)
                    i -= 1;

                if (i < 0) yield break;

                indices[i] += 1;

                for (int j = i + 1; j < r; j += 1)
                    indices[j] = indices[j - 1] + 1;

                yield return indices.Select(x => pool[x]).ToList();
            }
        }
    }
}