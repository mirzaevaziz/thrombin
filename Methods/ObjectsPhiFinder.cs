using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Methods
{
    class ObjectsPhiFinder
    {
        public class ObjectsPhiFinderResult
        {
            public int ObjectIndex { get; set; }
            public decimal Value { get; set; }
        }
        public static ConcurrentDictionary<int, ObjectsPhiFinderResult> Find(ObjectSet set, MetricCalculateFunctionDelegate distFunc, List<int> activeFeatures)
        {
            var result = new ConcurrentDictionary<int, ObjectsPhiFinderResult>();

            var dist = Utils.DistanceUtils.FindAllDistance(set, distFunc, activeFeatures);

            Parallel.ForEach(set.Objects, obj =>
            {
                var param = set.Objects.Select(s => new Criterions.FirstCriterion.FirstCriterionParameter()
                {
                    ClassValue = s.ClassValue.Value,
                    ObjectIndex = s.Index,
                    Distance = dist[obj.Index, s.Index]

                });

                var firstCriterionValue = Criterions.FirstCriterion.Find(
                    param,
                    obj.ClassValue.Value
                );

                // Find how many obj's friends in [c1,c2]
                var classCountInInterval = 0;
                // Find how many obj's enemies in [c1,c2]
                var nonClassCountInInterval = 0;
                // Find how many obj's friends
                decimal classCount = 0;
                // Find how many obj's enemies
                decimal nonClassCount = 0;
                foreach (var p in param)
                {
                    if (p.ClassValue == obj.ClassValue)
                    {
                        classCount++;
                        if (p.Distance <= firstCriterionValue.Distance)
                            classCountInInterval++;
                    }
                    else
                    {
                        nonClassCount++;
                        if (p.Distance <= firstCriterionValue.Distance)
                            nonClassCountInInterval++;
                    }
                }
                var o1 = classCountInInterval / classCount;
                var o2 = nonClassCountInInterval / nonClassCount;
                result[obj.Index] = new ObjectsPhiFinderResult()
                {
                    ObjectIndex = obj.Index,
                    Value = o1 * (1 - o2)
                };
            });

            return result;
        }
    }
}