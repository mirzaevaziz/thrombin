using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using thrombin.Helpers;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Methods
{
    class FindAllFeaturesByPhi
    {

        class FeaturePhi
        {
            public int FeatureIndex { get; set; }
            public decimal R { get; set; }
            public ConcurrentDictionary<int, Methods.ObjectsPhiFinder.ObjectsPhiFinderResult> PhiList { get; set; }
            public List<int> ActiveFeatures { get; internal set; }
        }

        public static List<int> Find(ObjectSet set, MetricCalculateFunctionDelegate distFunc, Logger log, List<int> activeFeatures, HashSet<int> candidateFeatures = null)
        {
            log.WriteLine("FindAllFeaturesByPhi", $"=====Started FindAllFeaturesByPhi at {DateTime.Now}", true);
            if (candidateFeatures == null || candidateFeatures.Count == 0)
            {
                candidateFeatures = new HashSet<int>();
                for (int i = 0; i < set.Features.Length; i++)
                {
                    if (activeFeatures.Contains(i)) continue;

                    candidateFeatures.Add(i);
                }
            }

            var prevPhi = new FeaturePhi()
            {
                FeatureIndex = -1,
                R = -1,
                PhiList = Methods.ObjectsPhiFinder.Find(set, distFunc, activeFeatures)
            };

            do
            {
                var rList = new BlockingCollection<FeaturePhi>();
                Parallel.ForEach(candidateFeatures, i =>
                {
                    // log.WriteLine($"Finding phi for feature {i}");
                    var result = new FeaturePhi() { FeatureIndex = i };
                    var ft = new List<int>(activeFeatures);
                    ft.Add(i);
                    result.PhiList = Methods.ObjectsPhiFinder.Find(set, distFunc, ft);
                    result.ActiveFeatures = ft;
                    if (result.PhiList.Count() == 0)
                        return;
                    rList.Add(result);
                });
                rList.CompleteAdding();
                FeaturePhi maxPhi = null;
                foreach (var item in rList)
                {
                    item.R = item.PhiList.Count(w => w.Value.Value >= prevPhi.PhiList[w.Key].Value) / (decimal)set.Objects.Length;
                    // log.WriteLine("FindAllFeaturesByPhi", $"Feature {item.FeatureIndex:000} \t R = {item.R:0.000000}, MaxR = {maxPhi?.R:0.000000}");
                    if (item.R > 0.5M && (maxPhi == null || maxPhi.R < item.R))
                    {
                        maxPhi = item;
                        if (item.R == 1) break;
                    }
                }

                if (maxPhi == null || activeFeatures.Contains(maxPhi.FeatureIndex))
                    break;
                prevPhi = maxPhi;
                candidateFeatures.Remove(maxPhi.FeatureIndex);
                activeFeatures.Add(maxPhi.FeatureIndex);
                log.WriteLine("FindAllFeaturesByPhi", $"\tFound {maxPhi.R} at index {maxPhi.FeatureIndex}. Active features [{string.Join(',', maxPhi.ActiveFeatures)}]");
            } while (candidateFeatures.Count > 0);
            var sb = new StringBuilder();

            sb.AppendLine("Features:");
            foreach (var i in activeFeatures)
            {
                var ft = set.Features[i];
                sb.AppendLine($"\t{i}. {ft}");
            }
            log.WriteLine("FindAllFeaturesByPhi", $"{sb}=====Ended FindAllFeaturesByPhi at {DateTime.Now}", true);
            return activeFeatures;
        }
    }
}