using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using thrombin.Helpers;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindAllFeaturesByRs
    {
        public class FindAllFeaturesByRsResult
        {
            public Dictionary<int, decimal> RSList { get; set; }
            public int FeatureIndex { get; set; }
            public decimal R { get; set; }
            public IEnumerable<int> ActiveFeatures { get; set; }
        }

        public static List<int> Find(ObjectSet set, Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult> weights, Logger log, List<int> activeFeatures, HashSet<int> candidateFeatures = null)
        {
            log.WriteLine("FindAllFeaturesByRsResult", $"=====Started FindAllFeaturesByRsResult at {DateTime.Now}", true);
            if (candidateFeatures == null || candidateFeatures.Count == 0)
            {
                candidateFeatures = new HashSet<int>();
                for (int i = 0; i < set.Features.Length; i++)
                {
                    if (activeFeatures.Contains(i)) continue;

                    candidateFeatures.Add(i);
                }
            }

            candidateFeatures.ExceptWith(activeFeatures);

            var prevPhi = new FindAllFeaturesByRsResult()
            {
                FeatureIndex = -1,
                R = 1,
                RSList = Methods.GeneralizedAssessment.FindNonContiniousFeature(set, weights, activeFeatures)
            };

            do
            {
                var rList = new BlockingCollection<FindAllFeaturesByRsResult>();
                Parallel.ForEach(candidateFeatures, i =>
                {
                    // log.WriteLine($"Finding phi for feature {i}");
                    var result = new FindAllFeaturesByRsResult() { FeatureIndex = i };
                    var ft = new List<int>(activeFeatures);
                    ft.Add(i);
                    result.RSList = Methods.GeneralizedAssessment.FindNonContiniousFeature(set, weights, ft);
                    result.ActiveFeatures = ft;
                    if (result.RSList.Count == 0)
                        return;
                    rList.Add(result);
                });
                rList.CompleteAdding();
                FindAllFeaturesByRsResult minR = null;
                foreach (var item in rList)
                {
                    int k = 0, ck = 1;
                    decimal k_sum = 0, ck_sum = 0;

                    for (int i = 0; i < set.Objects.Length; i++)
                    {
                        if (set.Objects[i].ClassValue == set.ClassValue)
                        {
                            k_sum += item.RSList[i];
                            k++;
                        }
                        else
                        {
                            ck_sum += item.RSList[i];
                            ck++;
                        }
                    }

                    k_sum = k_sum / k;
                    ck_sum = ck_sum / ck;


                    decimal tetta = item.RSList.Sum(s => (set.Objects[s.Key].ClassValue == set.ClassValue) ? Math.Abs(s.Value - k_sum) : Math.Abs(s.Value - ck_sum));
                    decimal gamma = item.RSList.Sum(s => (set.Objects[s.Key].ClassValue == set.ClassValue) ? Math.Abs(s.Value - ck_sum) : Math.Abs(s.Value - k_sum));

                    item.R = tetta / gamma;

                    if (minR == null || minR.R > item.R)
                    {
                        minR = item;
                    }
                }

                if (minR == null || activeFeatures.Contains(minR.FeatureIndex) || Math.Abs(prevPhi.R - minR.R) < 0.0001M)
                    break;
                prevPhi = minR;
                candidateFeatures.Remove(minR.FeatureIndex);
                activeFeatures.Add(minR.FeatureIndex);
                log.WriteLine("FindAllFeaturesByRsResult", $"\tFound {minR.R} at index {minR.FeatureIndex}. Active features [{string.Join(',', minR.ActiveFeatures)}]");
            } while (candidateFeatures.Count > 0);
            var sb = new StringBuilder();

            sb.AppendLine("Features:");
            foreach (var i in activeFeatures)
            {
                var ft = set.Features[i];
                sb.AppendLine($"\t{i}. {ft}");
            }
            log.WriteLine("FindAllFeaturesByRsResult", $"{sb}=====Ended FindAllFeaturesByRsResult at {DateTime.Now}", true);
            return activeFeatures;
        }
    }
}