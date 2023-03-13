using System.Collections.Generic;
using System.Linq;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Utils
{
    public class DistanceUtils
    {
        /// <summary>
        /// Getting all objects' distance by active features
        /// </summary>
        /// <param name="set"></param>
        /// <param name="distFunc"></param>
        /// <param name="activeFeatures"></param>
        /// <returns></returns>
        public static DistanceList FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc, IEnumerable<int> activeFeatures)
        {
            var dist = new DistanceList(set.Objects.Length, set.Objects.Length);

            // if (isParallel)
            //     Parallel.For(0, set.Objects.Length, i =>
            //     {
            //         dist[i, i] = 0M;
            //         for (int j = i + 1; j < set.Objects.Length; j++)
            //         {
            //             dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, activeFeatures);
            //             dist[j, i] = dist[i, j];
            //         }
            //     });
            // else
            {
                for (int i = 0; i < set.Objects.Length; i++)
                {
                    dist[i, i] = 0M;
                    for (int j = i + 1; j < set.Objects.Length; j++)
                    {
                        dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, activeFeatures);
                        dist[j, i] = dist[i, j];
                    }
                }
            }

            return dist;
        }


        public class DistanceList
        {
            public List<List<decimal>> Array { get; set; }

            public DistanceList(int rowCount, int colCount)
            {
                Array = new List<List<decimal>>();
                for (int i = 0; i < rowCount; i++)
                {
                    var r = Enumerable.Range(0, colCount).Select(s => 0M).ToList();
                    Array.Add(r);
                }
            }

            public decimal this[int i, int j]
            {
                get
                {
                    return Array[i][j];
                }
                set
                {
                    Array[i][j] = value;
                }
            }
        }

        /// <summary>
        /// Getting all objects' distance by all features
        /// </summary>
        /// <param name="set"></param>
        /// <param name="distFunc"></param>
        /// <returns></returns>
        public static DistanceList FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc)
        {
            return FindAllDistance(set, distFunc, Enumerable.Range(0, set.Features.Length));
        }

        public static DistanceList FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc, HashSet<int> excludedObjects)
        {
            if (excludedObjects.Count == 0)
                return FindAllDistance(set, distFunc);

            DistanceList dist = new DistanceList(set.Objects.Length, set.Objects.Length);

            for (int i = 0; i < set.Objects.Length; i++)
            {
                if (excludedObjects.Contains(i))
                {
                    for (int j = 0; j < set.Objects.Length; j++)
                    {
                        dist[i, j] = -1;
                        dist[j, i] = -1;
                    }
                }
                else
                {
                    dist[i, i] = 0M;
                    for (int j = i + 1; j < set.Objects.Length; j++)
                    {
                        if (excludedObjects.Contains(j))
                            continue;

                        dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, Enumerable.Range(0, set.Features.Length));
                        dist[j, i] = dist[i, j];
                    }
                }
            }

            return dist;
        }
    }
}