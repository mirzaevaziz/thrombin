using System;
using System.Collections.Generic;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Metrics
{
    public class Chebyshev : IMetric
    {
        public decimal Calculate(ObjectInfo obj1, ObjectInfo obj2, Feature[] features, IEnumerable<int> activeFeaturesIndexes)
        {
            var result = -1M;

            foreach (var i in activeFeaturesIndexes)
            {
                if (features[i].IsContinuous)
                {
                    var r = obj1[i] - obj2[i];
                    if (r < 0) r *= -1;
                    if (result == -1 || result < r)
                        result = r;
                }
                else
                    throw new NotImplementedException();
            }

            return result;
        }

        public bool CanCalculate(Feature[] features)
        {
            return features.All(f => f.IsContinuous);
        }
    }
}