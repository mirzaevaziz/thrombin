using System;
using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Metrics
{
    public class Euclidean
    {
        public decimal Calculate(ObjectInfo obj1, ObjectInfo obj2, Feature[] features, IEnumerable<int> activeFeaturesIndexes)
        {
            var result = 0M;

            foreach (var i in activeFeaturesIndexes)
            {
                if (features[i].IsContinuous)
                {
                    var r = obj1[i] - obj2[i];
                    result += r * r;
                }
                else
                    throw new NotImplementedException();
            }

            return (decimal)Math.Sqrt((double)result);
        }

        public bool CanCalculate(IEnumerable<Feature> features)
        {
            return features.All(f => f.IsContinuous);
        }
    }

    class Juravlev
    {
        public decimal Calculate(ObjectInfo obj1, ObjectInfo obj2, List<Feature> features, IEnumerable<int> activeFeaturesIndexes)
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

        public bool CanCalculate(List<Feature> features)
        {
            return true;
        }
    }
}