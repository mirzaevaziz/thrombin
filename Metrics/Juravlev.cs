using System.Collections.Generic;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Metrics
{
    public class Juravlev : IMetric
    {
        public decimal Calculate(ObjectInfo obj1, ObjectInfo obj2, Feature[] features, IEnumerable<int> activeFeaturesIndexes)
        {
            var result = 0M;

            foreach (var i in activeFeaturesIndexes)
            {
                if (features[i].IsContinuous)
                {
                    var r = obj1[i] - obj2[i];
                    if (r < 0) r *= -1;

                    result += r;
                }
                else if (obj1[i] != obj2[i])
                    result += 1;
            }

            return result;
        }

        public bool CanCalculate(Feature[] features)
        {
            return true;
        }
    }
}