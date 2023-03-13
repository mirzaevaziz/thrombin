using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using thrombin.Helpers;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindFirstPairFeatureByDominance
    {
        public class FindFirstPairFeatureResult
        {
            public decimal Value { get; set; }
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

            log.WriteLine("FindFirstPairFeatureByDominance", $"=====Started FindFirstPairFeatureByDominance at {DateTime.Now}", true);

            var queue = new BlockingCollection<FindFirstPairFeatureResult>();
            decimal classCount = set.Objects.Count(w => w.ClassValue == set.ClassValue);
            decimal nonClassCount = set.Objects.Count(w => w.ClassValue != set.ClassValue);

            Parallel.ForEach(activeFeatures.Combinations(2), ft =>
            {
                var dist = Utils.DistanceUtils.FindAllDistance(set, distFunc, ft);

                var result = new FindFirstPairFeatureResult()
                {
                    Value = 0,
                    Features = ft,
                };

                for (int objCounter = 0; objCounter < set.Objects.Length; objCounter++)
                {
                    decimal k = 0, ck = 0, max = 0, objectClassValue = set.Objects[objCounter].ClassValue.Value;

                    var distList = set.Objects.Select(s => new
                    {
                        s.Index,
                        Distance = dist[objCounter, s.Index],
                        ClassValue = s.ClassValue
                    }).OrderBy(o => o.Distance).ToList();

                    for (int sObjCounter = 0; sObjCounter < set.Objects.Length; sObjCounter++)
                    {
                        if (distList[sObjCounter].ClassValue == objectClassValue)
                        {
                            k++;
                        }
                        else
                        {
                            ck++;
                        }
                        if (sObjCounter < set.Objects.Length - 1 && distList[sObjCounter].Distance == distList[sObjCounter + 1].Distance) continue;

                        decimal val = -1;
                        if (objectClassValue == set.ClassValue)
                        {
                            val = ((k / classCount) / ((k / classCount) + (ck / nonClassCount))) * (k / classCount);
                        }
                        else
                        {
                            val = ((ck / nonClassCount) / ((k / classCount) + (ck / nonClassCount))) * (ck / nonClassCount);
                        }

                        if (max <= val)
                        {
                            max = val;
                        }
                    }
                    result.Value += max;
                }

                queue.Add(result);
            });
            queue.CompleteAdding();
            decimal max = 0;
            List<int> result = null;
            foreach (var item in queue)
            {
                log.WriteLine("FindFirstPairFeatureByDominance", $"Found for features [{string.Join(", ", item.Features)}] => {item.Value}");
                if (max < item.Value)
                {
                    max = item.Value;
                    result = item.Features;
                    log.WriteLine("FindFirstPairFeatureByDominance", $"\tMax {max}");
                }
            }

            log.WriteLine("FindFirstPairFeatureByDominance", $"First pair is: {string.Join(", ", result)}", true);
            log.WriteLine("FindFirstPairFeatureByDominance", $"=====Ended FindFirstPairFeatureByDominance at {DateTime.Now}", true);
            return result;
        }
    }
}