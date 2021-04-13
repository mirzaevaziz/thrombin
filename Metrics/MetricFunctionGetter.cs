using System;
using System.Collections.Generic;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Metrics
{
    public delegate decimal MetricCalculateFunctionDelegate(ObjectInfo obj1, ObjectInfo obj2, Feature[] features, IEnumerable<int> activeFeaturesIndexes);

    static public class MetricFunctionGetter
    {
        public static MetricCalculateFunctionDelegate GetMetric(ObjectSet set, string forMethod)
        {
            var distFuncs = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                                .Where(x => typeof(IMetric).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract).ToList();

            IMetric distFunc = null;
            while (distFunc == null)
            {
                System.Console.WriteLine($"Please, choose metric for {forMethod} (default = 0):");
                int i = 0;
                distFuncs.ForEach(provider => System.Console.WriteLine($"\t{i++:000}: {provider.Name}"));
                int.TryParse(Console.ReadLine(), out i);
                if (i >= 0 && i < distFuncs.Count)
                {
                    distFunc = (IMetric)Activator.CreateInstance(distFuncs[i]);
                    if (!distFunc.CanCalculate(set.Features))
                    {
                        System.Console.WriteLine("Chosen metric cannot calculate for this object set.");
                        distFunc = null;
                    }
                    else
                    {
                        System.Console.WriteLine($"Metric is {distFuncs[i].Name}");
                    }
                }
            }

            return distFunc.Calculate;
        }
    }
}