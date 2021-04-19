using System.Collections.Generic;
using System.Linq;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Utils
{
    class DistanceUtils
    {
        /// <summary>
        /// Getting all objects' distance by active features
        /// </summary>
        /// <param name="set"></param>
        /// <param name="distFunc"></param>
        /// <param name="activeFeatures"></param>
        /// <returns></returns>
        public static decimal[,] FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc, IEnumerable<int> activeFeatures)
        {
            decimal[,] dist = new decimal[set.Objects.Length, set.Objects.Length];

            for (int i = 0; i < set.Objects.Length; i++)
            {
                dist[i, i] = 0M;
                for (int j = i + 1; j < set.Objects.Length; j++)
                {
                    dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, activeFeatures);
                    dist[j, i] = dist[i, j];
                }
            }

            return dist;
        }        

        /// <summary>
        /// Getting all objects' distance by active features
        /// </summary>
        /// <param name="set"></param>
        /// <param name="distFunc"></param>
        /// <param name="activeFeatures"></param>
        /// <returns></returns>
        public static DistanceAndRadius FindAllDistanceAndRadius(ObjectSet set, MetricCalculateFunctionDelegate distFunc, IEnumerable<int> activeFeatures, HashSet<int> excludedObjects)
        {
            decimal[,] dist = new decimal[set.Objects.Length, set.Objects.Length];

            var minDist = new Dictionary<int, decimal>();

            for (int i = 0; i < set.Objects.Length; i++)
                minDist[i] = -1M;

            if (excludedObjects?.Count > 0)
            {
                foreach (var ex in excludedObjects)
                {
                    for (int j = 0; j < set.Objects.Length; j++)
                    {
                        dist[ex, j] = -1;
                        dist[j, ex] = -1;
                    }
                }

                for (int i = 0; i < set.Objects.Length; i++)
                {
                    if (excludedObjects.Contains(i)) continue;

                    dist[i, i] = 0M;
                    for (int j = i + 1; j < set.Objects.Length; j++)
                    {
                        if (excludedObjects.Contains(j)) continue;

                        dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, activeFeatures);
                        dist[j, i] = dist[i, j];

                        if (set.Objects[i].ClassValue != set.Objects[j].ClassValue)
                        {
                            if (minDist[i] > dist[i, j] || minDist[i] == -1M)
                                minDist[i] = dist[i, j];

                            if (minDist[j] > dist[i, j] || minDist[j] == -1M)
                                minDist[j] = dist[i, j];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < set.Objects.Length; i++)
                {
                    dist[i, i] = 0M;
                    for (int j = i + 1; j < set.Objects.Length; j++)
                    {
                        dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, activeFeatures);
                        dist[j, i] = dist[i, j];

                        if (set.Objects[i].ClassValue != set.Objects[j].ClassValue)
                        {
                            if (minDist[i] > dist[i, j] || minDist[i] == -1M)
                                minDist[i] = dist[i, j];

                            if (minDist[j] > dist[i, j] || minDist[j] == -1M)
                                minDist[j] = dist[i, j];
                        }
                    }
                }
            }

            return new DistanceAndRadius
            {
                Distances = dist,
                Radiuses = minDist
            };
        }

        /// <summary>
        /// Getting all objects' distance by all features
        /// </summary>
        /// <param name="set"></param>
        /// <param name="distFunc"></param>
        /// <returns></returns>
        public static decimal[,] FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc)
        {
            return FindAllDistance(set, distFunc, Enumerable.Range(0, set.Features.Length));
        }

        public static decimal[,] FindAllDistance(ObjectSet set, MetricCalculateFunctionDelegate distFunc, HashSet<int> excludedObjects)
        {
            if (excludedObjects.Count == 0)
                return FindAllDistance(set, distFunc);

            decimal[,] dist = new decimal[set.Objects.Length, set.Objects.Length];

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