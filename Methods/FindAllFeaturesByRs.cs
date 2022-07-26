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
            public string Note { get; set; }
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
                    if (weights[i].FeatureContribute.Count < 2) return;

                    var result = new FindAllFeaturesByRsResult() { FeatureIndex = i };
                    var ft = new List<int>(activeFeatures);
                    ft.Add(i);
                    result.RSList = new Dictionary<int, decimal>();
                    foreach (var objInd in prevPhi.RSList.Keys)
                    {
                        result.RSList[objInd] = prevPhi.RSList[objInd] + weights[i].FeatureContribute[set.Objects[objInd][i]];
                    }
                    // result.RSList = Methods.GeneralizedAssessment.FindNonContiniousFeature(set, weights, ft);
                    result.ActiveFeatures = ft;
                    if (result.RSList.Count == 0)
                        return;
                    rList.Add(result);
                });
                rList.CompleteAdding();
                FindAllFeaturesByRsResult minR = null;
                foreach (var item in rList)
                {
                    int k = 0, ck = 0;
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

                    decimal tetta = 0, gamma = 0;

                    foreach (var r in item.RSList)
                    {
                        if (set.Objects[r.Key].ClassValue == set.ClassValue)
                        {
                            tetta += Math.Abs(k_sum - r.Value);
                            gamma += Math.Abs(ck_sum - r.Value);
                        }
                        else
                        {
                            tetta += Math.Abs(ck_sum - r.Value);
                            gamma += Math.Abs(k_sum - r.Value);
                        }
                    }

                    if (gamma == 0) continue;

                    item.R = tetta / gamma;

                    if (minR == null || minR.R > item.R || (minR.R == item.R && weights[minR.FeatureIndex].Value < weights[item.FeatureIndex].Value))
                    {
                        minR = item;
                        minR.Note = $"Feature {minR.FeatureIndex}, k_sum = {k_sum}, ck_sum = {ck_sum}, tetta = {tetta}, gamma = {gamma}, value = {minR.R}\n";
                    }
                }

                // log.WriteLine("TettaGamma.txt", "===================");
                // foreach (var item in rList.OrderBy(o => o.R))
                // {
                //     log.WriteLine("TettaGamma.txt", $"{item.FeatureIndex}\tvalue={item.R}");
                // }

                if (minR == null || activeFeatures.Contains(minR.FeatureIndex) || prevPhi.R <= minR.R || Math.Abs(prevPhi.R - minR.R) < 0.0001M) //   
                    break;

                prevPhi = minR;
                candidateFeatures.Remove(minR.FeatureIndex);
                activeFeatures.Add(minR.FeatureIndex);
                log.WriteLine("FindAllFeaturesByRsResult", $"\tFound {minR.R} at index {minR.FeatureIndex}. Active features ({minR.ActiveFeatures.Count()})[{string.Join(',', minR.ActiveFeatures)}]{Environment.NewLine}\t{minR.Note}");
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