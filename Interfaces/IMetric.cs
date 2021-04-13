using System.Collections.Generic;
using thrombin.Models;

namespace thrombin.Interfaces
{
    internal interface IMetric
    {
        decimal Calculate(ObjectInfo obj1, ObjectInfo obj2, Feature[] features, IEnumerable<int> activeFeaturesIndexes);
        bool CanCalculate(Feature[] features);
    }
}