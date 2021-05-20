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
    public class FindAllFeaturesByRsSphere
    {
        public class FindAllFeaturesByRsResult
        {
            public Dictionary<int, decimal> RSList { get; set; }
            public int FeatureIndex { get; set; }
            public int R { get; set; }
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
                    decimal[,] dist = new decimal[set.Objects.Length, set.Objects.Length];

                    var minDist = new Dictionary<int, decimal>();

                    for (int i = 0; i < set.Objects.Length; i++)
                        minDist[i] = -1M;

                    for (int i = 0; i < set.Objects.Length; i++)
                    {
                        dist[i, i] = 0M;
                        for (int j = i + 1; j < set.Objects.Length; j++)
                        {
                            dist[i, j] = Math.Abs(item.RSList[i] - item.RSList[j]);
                            dist[j, i] = dist[i, j];

                            if (set.Objects[i].ClassValue != set.Objects[j].ClassValue)
                            {
                                if (minDist[i] > dist[i, j] || minDist[i] == -1M)
                                    minDist[i] = dist[i, j];

                                if (minDist[j] > dist[i, j] || minDist[j] == -1M)
                                    minDist[j] = dist[i, j];
                            }
                        }
                    }

                    for (int i = 0; i < set.Objects.Length; i++)
                    {
                        for (int j = 0; j < set.Objects.Length; j++)
                        {
                            if (dist[i, j] < minDist[i])
                                item.R++;
                        }
                    }

                    if (minR == null || minR.R < item.R)
                    {
                        minR = item;
                    }
                }

                if (minR == null || activeFeatures.Contains(minR.FeatureIndex) || prevPhi.R > minR.R) //  || Math.Abs(prevPhi.R - minR.R) < 0.0001M
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