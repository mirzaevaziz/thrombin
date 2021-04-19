using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using thrombin.Helpers;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindFirstPairFeatureByRelativesCount
    {
        public class FindFirstPairFeatureByRelativesCountResult
        {
            public int RelativesCount { get; set; }
            public List<int> Features { get; internal set; }
        }

        public static List<int> Find(ObjectSet set, MetricCalculateFunctionDelegate distFunc, IEnumerable<int> activeFeatures, Helpers.Logger log)
        {
            if (set is null)
            {
                throw new System.ArgumentNullException(nameof(set));
            }

            if (distFunc is null)
            {
                throw new System.ArgumentNullException(nameof(distFunc));
            }

            log.WriteLine("FindFirstPairFeatureByRelativesCount", $"===== FindFirstPairFeatureByRelativesCount Started at {DateTime.Now}", true);

            var queue = new BlockingCollection<FindFirstPairFeatureByRelativesCountResult>();

            Parallel.ForEach(activeFeatures.Combinations(2), ft =>
            {
                decimal[,] dist = new decimal[set.Objects.Length, set.Objects.Length];

                var minDist = new Dictionary<int, decimal>();

                for (int i = 0; i < set.Objects.Length; i++)
                    minDist[i] = -1M;

                for (int i = 0; i < set.Objects.Length; i++)
                {
                    dist[i, i] = 0M;
                    for (int j = i + 1; j < set.Objects.Length; j++)
                    {
                        dist[i, j] = distFunc(set.Objects[i], set.Objects[j], set.Features, ft);
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
                var result = new FindFirstPairFeatureByRelativesCountResult()
                {
                    RelativesCount = 0,
                    Features = ft,
                };

                for (int i = 0; i < set.Objects.Length; i++)
                {
                    for (int j = 0; j < set.Objects.Length; j++)
                    {
                        if (dist[i, j] < minDist[i])
                            result.RelativesCount++;
                    }
                }

                queue.Add(result);
            });
            queue.CompleteAdding();
            var max = 0;
            List<int> result = null;
            foreach (var item in queue)
            {
                log.WriteLine("FindFirstPairFeatureByRelativesCount", $"Found for features [{string.Join(", ", item.Features)}] => {item.RelativesCount}");
                if (max < item.RelativesCount)
                {
                    max = item.RelativesCount;
                    result = item.Features;
                    log.WriteLine("FindFirstPairFeatureByRelativesCount", $"\tMax {max}");
                }
            }
            if (result != null)
                log.WriteLine("FindFirstPairFeatureByRelativesCount", $"First pair is: {string.Join(", ", result)}", true);
            log.WriteLine("FindFirstPairFeatureByRelativesCount", $"===== FindFirstPairFeatureByRelativesCount Ended at {DateTime.Now}", true);
            return result;
        }
    }
}