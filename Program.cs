using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace thrombin
{
    class Program
    {
        static void Main(string[] args)
        {
            // OldMethod();
            // FindingRSSet(1000);
            // RS1000FindByRelativeCount();
            // RS1000FindByDominance();
            // ManualFirstPair();
            // TestManualFirstPair();
            // Method2D();
            // Method2DByRS();
            // FindSimilarObjects();
            // MethodFeatureSetBySphere();

            // KNNMethod();

            // ClusterByFeature();

            // var trainSet = new Data.Train.FizmatNNSet().GetSet();
            // System.Console.WriteLine(trainSet);

            // var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            // Parallel.For(0, trainSet.Features.Length, i =>
            // {
            //     trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
            //     {
            //         ClassValue = s.ClassValue.Value,
            //         FeatureValue = s[i],
            //         ObjectIndex = s.Index
            //     }), trainSet.ClassValue);
            // });

            // using (var file = new StreamWriter($"{trainSet.Name} set feature weights.txt"))
            // {
            //     foreach (var item in trainSetFeatureWeights.OrderByDescending(o => o.Key))
            //     {
            //         file.WriteLine(item);
            //     }
            // }

            var trainSet = Models.ObjectSet.FromFileData(Path.Combine("Data", "RS", "rs", "ThrombinSet New Rs DataSet.txt"));
            System.Console.WriteLine(trainSet);

            var testSet = Models.ObjectSet.FromFileData(Path.Combine("Data", "RS", "rs", "ThrombinTestSet New Rs DataSet.txt"));
            System.Console.WriteLine(testSet);

            var featureMinMax = Enumerable.Range(0, trainSet.Features.Length).Select(s => new
            {
                Index = s,
                Min = trainSet.Objects.Min(m => m.Data[s]),
                Max = trainSet.Objects.Max(m => m.Data[s])
            }).ToList();

            foreach (var ft in featureMinMax)
            {
                foreach (var obj in trainSet.Objects)
                {
                    obj[ft.Index] = (obj[ft.Index] - ft.Min) / (ft.Max - ft.Min) + 0.000001M;
                }
                foreach (var obj in testSet.Objects)
                {
                    obj[ft.Index] = (obj[ft.Index] - ft.Min) / (ft.Max - ft.Min) + 0.000001M;
                }
            }

            for (int i = 0; i < trainSet.Features.Length; i++)
            {
                using (var file = new StreamWriter($"Interval criterion for feature {i}"))
                {
                    var result = Criterions.IntervalCriterion.Find(trainSet.Objects.Select(s => new Criterions.IntervalCriterion.IntervalCriterionParameter
                    {
                        ClassValue = s.ClassValue.Value,
                        Distance = s[i],
                        ObjectIndex = s.Index
                    }), trainSet.ClassValue);

                    file.WriteLine("Train=====================");
                    foreach (var item in result.OrderBy(o => o.ObjectValueStart))
                    {
                        file.WriteLine(item);
                    }

                    result = Criterions.IntervalCriterion.Find(testSet.Objects.Select(s => new Criterions.IntervalCriterion.IntervalCriterionParameter
                    {
                        ClassValue = s.ClassValue.Value,
                        Distance = s[i],
                        ObjectIndex = s.Index
                    }), trainSet.ClassValue);

                    file.WriteLine("Test=====================");
                    foreach (var item in result.OrderBy(o => o.ObjectValueStart))
                    {
                        file.WriteLine(item);
                    }
                }
            }

            // var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "For first pair");

            // var logger = new Helpers.Logger("FindFirstPairFeatureByRelativesCount");
            // var firstPair = Methods.FindFirstPairFeatureByRelativesCount.Find(trainSet, distFunc, Enumerable.Range(0, trainSet.Features.Length), logger);

            // var activeFeatures = Methods.FindAllFeaturesByPhi.Find(trainSet, distFunc, logger, firstPair);

            // trainSet.ToFileData("Thrombin train set normed set.txt");
            // testSet.ToFileData("Thrombin test set normed.txt");

            // // var activeFeatures = Enumerable.Range(0, testSet.Features.Length);
            // // var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "kNN");
            // for (int k = 1; k < 79; k += 2)
            // {
            //     System.Console.WriteLine($"========= {k} ===============");
            //     decimal k1 = 0, k2 = 0;
            //     var knn = new HashSet<int>();
            //     for (int i = 0; i < testSet.Objects.Length; i++)
            //     {
            //         var dist = trainSet.Objects.ToDictionary(k => k.Index, v => distFunc(testSet.Objects[i], v, testSet.Features, activeFeatures)).OrderBy(o => o.Value).Take(k);

            //         var k1Count = dist.Count(w => trainSet.Objects[w.Key].ClassValue == 1);
            //         var k2Count = dist.Count(w => trainSet.Objects[w.Key].ClassValue != 1);
            //         if (k1Count == k2Count)
            //         {
            //             System.Console.WriteLine($"Error on test {i}");
            //         }
            //         if (k1Count > k2Count && testSet.Objects[i].ClassValue == 1)
            //         {
            //             k1++;
            //             knn = knn.Union(dist.Select(s => s.Key)).ToHashSet();
            //         }
            //         else if (k1Count < k2Count && testSet.Objects[i].ClassValue != 1)
            //         {
            //             k2++;
            //             knn = knn.Union(dist.Select(s => s.Key)).ToHashSet();
            //         }
            //         // foreach (var item in dist)
            //         // {
            //         //     System.Console.WriteLine($"{item.Key} = {item.Value}");
            //         // }
            //     }

            //     System.Console.WriteLine(knn.Count);

            //     System.Console.WriteLine($"K1 {k1}\tK2 {k2}");

            //     System.Console.WriteLine($"All percent: {(k1 + k2) / testSet.Objects.Length}");

            //     System.Console.WriteLine($"Weighted percent: {(k1 / testSet.ClassObjectCount + k2 / testSet.NonClassObjectCount) / 2}");
            // }
        }

        private static void ClusterByFeature()
        {
            var trainSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects();
            var clusterSet = new BlockingCollection<ClusterSet>();

            Parallel.For(0, trainSet.Features.Length, j =>
            {
                var zeroValueObjects = trainSet.Objects.Where(w => w[j] == 0);
                var oneValueObjects = trainSet.Objects.Where(w => w[j] == 1);
                if (zeroValueObjects.All(a => a.ClassValue == trainSet.ClassValue))
                {
                    clusterSet.Add(new ClusterSet
                    {
                        ClassValue = trainSet.ClassValue,
                        FeatureIndex = j,
                        FeatureValue = 0,
                        Objects = zeroValueObjects.Select(s => s.Index).ToHashSet(),
                        ObjectsWeight = zeroValueObjects.Count() / (decimal)trainSet.ClassObjectCount
                    });
                }
                if (zeroValueObjects.All(a => a.ClassValue != trainSet.ClassValue))
                {
                    clusterSet.Add(new ClusterSet
                    {
                        ClassValue = trainSet.ClassValue + 1,
                        FeatureIndex = j,
                        FeatureValue = 0,
                        Objects = zeroValueObjects.Select(s => s.Index).ToHashSet(),
                        ObjectsWeight = zeroValueObjects.Count() / (decimal)trainSet.NonClassObjectCount
                    });
                }
                if (oneValueObjects.All(a => a.ClassValue == trainSet.ClassValue))
                {
                    clusterSet.Add(new ClusterSet
                    {
                        ClassValue = trainSet.ClassValue,
                        FeatureIndex = j,
                        FeatureValue = 1,
                        Objects = oneValueObjects.Select(s => s.Index).ToHashSet(),
                        ObjectsWeight = oneValueObjects.Count() / (decimal)trainSet.ClassObjectCount
                    });
                }
                if (oneValueObjects.All(a => a.ClassValue != trainSet.ClassValue))
                {
                    clusterSet.Add(new ClusterSet
                    {
                        ClassValue = trainSet.ClassValue + 1,
                        FeatureIndex = j,
                        FeatureValue = 1,
                        Objects = oneValueObjects.Select(s => s.Index).ToHashSet(),
                        ObjectsWeight = oneValueObjects.Count() / (decimal)trainSet.NonClassObjectCount
                    });
                }
            });

            using (var file = new StreamWriter("cluster result.txt"))
            {
                foreach (var cluster in clusterSet.OrderByDescending(o => o.ObjectsWeight))
                {
                    file.WriteLine($@"Feature {cluster.FeatureIndex}=========================
        Class value: {cluster.ClassValue}
        Feature value: {cluster.FeatureValue}
        Objects({cluster.Objects.Count}): {string.Join(", ", cluster.Objects)}
        Objects weight: {cluster.ObjectsWeight}");
                }
            }
        }

        class ClusterSet
        {
            public HashSet<int> Objects { get; set; }
            public int FeatureIndex { get; set; }
            public decimal FeatureValue { get; set; }
            public int ClassValue { get; set; }
            public decimal FeatureWeight { get; set; }
            public decimal ObjectsWeight { get; set; }
        }

        private static void KNNMethod()
        {
            var trainSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects();

            var testSet = new Data.Test.ThrombinTestSet().GetSet();

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method2D");
            var dir = new DirectoryInfo(Path.Combine("_Results", $"kNN Result {DateTime.Now: yyyyMMdd HHmmss}"));
            dir.Create();

            foreach (var testObject in testSet.Objects)
            {
                var dist = new Dictionary<int, decimal>();
                for (int i = 0; i < trainSet.Objects.Length; i++)
                {
                    dist[i] = distFunc(testObject, trainSet.Objects[i], trainSet.Features, Enumerable.Range(0, trainSet.Features.Length));
                }
                var orderedDist = dist.OrderBy(o => o.Value);
                for (int k = 1; k < 80; k++)
                    using (var resultFile = new StreamWriter(Path.Combine(dir.FullName, $"Result file {k}"), true))
                    {
                        var classValue = new int?[k];
                        int i = 0, ind = 0;
                        while (i < k)
                        {
                            if (!classValue[i].HasValue)
                                classValue[i] = trainSet.Objects[orderedDist.ElementAt(ind).Key].ClassValue;
                            else if (classValue[i] != trainSet.Objects[orderedDist.ElementAt(ind).Key].ClassValue)
                                classValue[i] = -1;

                            ind++;

                            // if (orderedDist.ElementAt(ind).Value != orderedDist.ElementAt(ind + 1).Value)
                            {
                                i++;
                            }
                        }

                        var k1 = classValue.Count(w => w == 1);
                        var k2 = classValue.Count(w => w == 2);

                        if (k1 != k2)
                        {
                            resultFile.WriteLine(k1 > k2 ? "A" : "I");
                        }
                        else
                        {
                            resultFile.WriteLine("N/A");
                        }
                    }
            }
        }

        private static void FindSimilarObjects()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");

            var trainSet = new Data.Train.ThrombinSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);
            var foundedIndexes = new HashSet<int>();
            for (int i = 0; i < trainSet.Objects.Length - 1; i++)
            {
                if (foundedIndexes.Contains(i)) continue;

                var found = false;
                for (int j = i + 1; j < trainSet.Objects.Length; j++)
                {
                    if (trainSet.Objects[i].EqualsByValues(trainSet.Objects[j]))
                    {
                        foundedIndexes.Add(j);
                        found = true;
                        logger.WriteLine("Similar Objects", $"Object {i:0000} = {j:00000} Class {trainSet.Objects[i].ClassValue} and {trainSet.Objects[j].ClassValue}");
                    }
                }
                if (found)
                    logger.WriteLine("Similar Objects", "==================");

            }
        }

        private static void MethodFeatureSetBySphere()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - MethodFeatureSetBySphere");

            var trainSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects();
            logger.WriteLine("Set info", trainSet.ToString(), true);
            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "MethodFeatureSetBySphere set");

            var newFeaturesCount = 3;
            var notSeenFeatures = Enumerable.Range(0, trainSet.Features.Length).ToList();
            for (int i = 0; i < newFeaturesCount; i++)
            {
                var activeFeatures = new HashSet<int>();
                var spheresList = new BlockingCollection<Tuple<int, Dictionary<int, int>>>();
                var prevSphere = new Dictionary<int, int>();
                while (notSeenFeatures.Count > 0)
                {
                    Parallel.For(0, notSeenFeatures.Count, ftIndex =>
                   {
                       var features = new List<int>(activeFeatures);
                       features.Add(ftIndex);

                       var dist = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, new HashSet<int>());

                       var spheres = Models.Sphere.FindAll(trainSet, dist, new HashSet<int>(), false);

                       spheresList.Add(new Tuple<int, Dictionary<int, int>>(ftIndex, spheres.ToDictionary(k => k.ObjectIndex.Value, v => v.Relatives.Count)));
                   });

                    if (activeFeatures.Count == 0)
                    {
                        var maxRelativesCount = 0;
                        var maxSphere = new Tuple<int, Dictionary<int, int>>(-1, new Dictionary<int, int>());
                        foreach (var sphere in spheresList)
                        {
                            var relativesCount = sphere.Item2.Sum(s => s.Value);
                            if (maxRelativesCount < relativesCount)
                            {
                                maxRelativesCount = relativesCount;
                                maxSphere = sphere;
                            }
                        }

                        if (maxRelativesCount == 0) break;

                        prevSphere = maxSphere.Item2;
                        activeFeatures.Append(maxSphere.Item1);
                        notSeenFeatures.Remove(maxSphere.Item1);
                    }
                    else
                    {
                        var maxRelativesCount = 0;
                        var maxSphere = new Tuple<int, Dictionary<int, int>>(-1, new Dictionary<int, int>());
                        foreach (var sphere in spheresList)
                        {
                            var relativesCount = sphere.Item2.Where(w => w.Value >= prevSphere[w.Key]).Count();
                            if (maxRelativesCount < relativesCount)
                            {
                                maxRelativesCount = relativesCount;
                                maxSphere = sphere;
                            }
                        }
                        if (maxRelativesCount <= 1 / 2M) break;

                        prevSphere = maxSphere.Item2;
                        activeFeatures.Append(maxSphere.Item1);
                        notSeenFeatures.Remove(maxSphere.Item1);
                    }
                }

                logger.WriteLine($"0{i}. Set of features.txt", string.Join(", ", activeFeatures));
                logger.WriteLine($"0{i}. Set of features.txt", $"Features count: {activeFeatures.Count}");
            }
        }

        private static void Method2DByRS()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");
            var testSet = new Data.Test.ThrombinTestSet().GetSet();
            var trainSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects(); //

            logger.WriteLine("Set info", trainSet.ToString(), true);

            // Finding all features weights
            var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            Parallel.For(0, trainSet.Features.Length, i =>
            {
                trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), trainSet.ClassValue);
            });

            var orderedFeatures = trainSetFeatureWeights.Where(w => w.Value.Value > 0).OrderByDescending(o => o.Value.Value).Select(s => s.Key).ToList();

            var newFeaturesCount = 50;
            var newFeatures = new Models.Feature[newFeaturesCount];

            var rs = new Dictionary<int, Dictionary<int, decimal>>();
            var testrs = new Dictionary<int, Dictionary<int, decimal>>();
            var boundary = new Dictionary<int, decimal>();
            for (int i = 0; i < newFeaturesCount; i++)
            {
                newFeatures[i] = new Models.Feature { Name = $"Ft of RS {i}", IsContinuous = true };
                var firstPair = new List<int>() { orderedFeatures[0] };
                var featuresSet = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
                logger.WriteLine($"0{i}. Set of features.txt", string.Join(", ", featuresSet));

                rs[i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), featuresSet);
                testrs[i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(testSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), featuresSet);

                var crit1Result = Criterions.FirstCriterion.Find(rs[i].Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = trainSet.Objects[s.Key].ClassValue.Value,
                    Distance = s.Value,
                    ObjectIndex = s.Key
                }), trainSet.ClassValue);

                boundary[i] = (crit1Result.Distance + rs[i].Where(w => w.Value > crit1Result.Distance).Min(m => m.Value)) / 2M;

                logger.WriteLine($"0{i}. Set of features.txt", $"Feature count is {featuresSet.Count()}\nCriterion1 result is {crit1Result}\nBoundary = {boundary[i]}");
                logger.WriteLine($"0{i}. Set of features.txt", string.Join("\n", rs[i].Values.Select(s => $"{s:0.000000}")));

                orderedFeatures = orderedFeatures.Except(featuresSet).ToList();

                rs[i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), featuresSet);
            }

            var rsObjects = new Models.ObjectInfo[trainSet.Objects.Length];
            for (int i = 0; i < trainSet.Objects.Length; i++)
            {
                rsObjects[i] = new Models.ObjectInfo
                {
                    Index = i,
                    Data = Enumerable.Range(0, newFeaturesCount).Select(s => rs[s][i]).ToArray(), //new decimal[3] { rs[0][i], rs[1][i], rs[2][i] },
                    ClassValue = trainSet.Objects[i].ClassValue,
                };
            }

            var testrsObjects = new Models.ObjectInfo[testSet.Objects.Length];
            for (int i = 0; i < testSet.Objects.Length; i++)
            {
                testrsObjects[i] = new Models.ObjectInfo
                {
                    Index = i,
                    Data = Enumerable.Range(0, newFeaturesCount).Select(s => testrs[s][i]).ToArray(), //new decimal[3] { rs[0][i], rs[1][i], rs[2][i] },
                    ClassValue = testSet.Objects[i].ClassValue,
                };
            }

            var rsSet = new Models.ObjectSet("Method3DByRs set", rsObjects, newFeatures, 1);
            rsSet.ToFileData($"{trainSet.Name} New Rs DataSet.txt");

            var testrsSet = new Models.ObjectSet("Method3DByRs set", testrsObjects, newFeatures, 1);
            testrsSet.ToFileData($"{testSet.Name} New Rs DataSet.txt");

            for (int i = 0; i < newFeaturesCount; i++)
            {
                var crit1Result = Criterions.FirstCriterion.Find(rsSet.Objects.Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    Distance = s[i],
                    ObjectIndex = s.Index
                }), rsSet.ClassValue);

                logger.WriteLine("Criterion 1 results", crit1Result.ToString());
                logger.WriteLine("Criterion 1 results", $"Objects count {rsSet.Objects.Length}");

                logger.WriteLine("Criterion 1 results", $"\tMin: {rsSet.Objects.Min(m => m[i])}, Max: {rsSet.Objects.Max(m => m[i])}");

                logger.WriteLine("Criterion 1 results", $"\tUnique values: {rsSet.Objects.Select(m => m[i]).Distinct().Count()}");

                var near = rsSet.Objects.Where(w => w[i] > crit1Result.Distance).Min(m => m[i]);
                logger.WriteLine("Criterion 1 results", $"\tNear: {near}, Boundary: {boundary[i]}");

                decimal k1 = rsSet.Objects.Where(w => w[i] > boundary[i] && w.ClassValue == 1).Count();
                decimal k1All = rsSet.Objects.Where(w => w.ClassValue == 1).Count();
                decimal k2 = rsSet.Objects.Where(w => w[i] < boundary[i] && w.ClassValue != 1).Count();
                decimal k2All = rsSet.Objects.Where(w => w.ClassValue != 1).Count();
                logger.WriteLine("Criterion 1 results", $"\tK1: {k1} ({k1All})");
                logger.WriteLine("Criterion 1 results", $"\tK2: {k2} ({k2All})");
                logger.WriteLine("Criterion 1 results", $"\tPercent: {((k1 + k2) / (decimal)rsSet.Objects.Length) * 100:00.00}%");

                logger.WriteLine("Criterion 1 results", $"===Test objects {testrsSet.Objects.Length}===");
                k1 = testrsSet.Objects.Where(w => w[i] > boundary[i] && w.ClassValue == 1).Count();
                k1All = testrsSet.Objects.Where(w => w.ClassValue == 1).Count();
                k2 = testrsSet.Objects.Where(w => w[i] < boundary[i] && w.ClassValue != 1).Count();
                k2All = testrsSet.Objects.Where(w => w.ClassValue != 1).Count();
                logger.WriteLine("Criterion 1 results", $"\tK1: {k1} ({k1All})\t{k1 / k1All * 100:00.00}%");
                logger.WriteLine("Criterion 1 results", $"\tK2: {k2} ({k2All})\t{k2 / k2All * 100:00.00}%");
                logger.WriteLine("Criterion 1 results", $"\tPercent: {((k1 + k2) / (decimal)testrsSet.Objects.Length) * 100:00.00}%,\tWeighted: {(k1 / k1All + k2 / k2All) / 2}");
            }

            // rsSet = Methods.NormilizingMinMax.Normalize(rsSet);
            // var excludedObjects = new HashSet<int>();

            // trainSet = rsSet;
            // var features = Enumerable.Range(0, newFeaturesCount);

            // var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method3DByRS set");
            // var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            // var spheres = Models.Sphere.FindAll(trainSet, distances.Distances, excludedObjects, true);
            // var noisyObjects = Methods.FindNoisyObjects.Find(trainSet, spheres, excludedObjects, logger);
            // excludedObjects.UnionWith(noisyObjects);
            // distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            // spheres = Models.Sphere.FindAll(trainSet, distances.Distances, excludedObjects, true);
            // var groups = Methods.FindAcquaintanceGrouping.Find(trainSet, spheres, excludedObjects);
            // var standartObject = Methods.FindStandartObjects.Find(trainSet, groups, spheres, excludedObjects, distances, logger);
            // logger.WriteLine("Result", $"Stability: {((trainSet.Objects.Length - noisyObjects.Count) / (decimal)trainSet.Objects.Length) * ((trainSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count)}");
            // logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            // logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            // logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");

            // foreach (var st in standartObject)
            // {
            //     logger.WriteLine("Result", $"{{{st}, {distances.Radiuses[st]}M}}");
            // }

            // logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");
        }

        private static void Method2D()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2D");

            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method2D");

            // Finding all features weights
            var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            Parallel.For(0, trainSet.Features.Length, i =>
            {
                trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), trainSet.ClassValue);
            });

            var orderedFeatures = trainSetFeatureWeights.OrderByDescending(o => o.Value.Value).Take(1500).Select(s => s.Key).ToList();

            var firstPair = new List<int>() { orderedFeatures[0] };
            var features1 = Methods.FindAllFeaturesByPhi.Find(trainSet, distFunc, logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
            logger.WriteLine("01. First set of features.txt", string.Join(", ", features1));

            orderedFeatures = orderedFeatures.Except(features1).ToList();
            firstPair = new List<int>() { orderedFeatures[0] };
            var features2 = Methods.FindAllFeaturesByPhi.Find(trainSet, distFunc, logger, firstPair, orderedFeatures.Skip(1).ToHashSet());

            logger.WriteLine("02. Second set of features.txt", string.Join(", ", features2));

            // var excludedObjects = new HashSet<int>();

            // var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            // var spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            // // var noisyObjects = Methods.FindNoisyObjects.Find(trainSet, spheres, excludedObjects, logger);
            // // excludedObjects.UnionWith(noisyObjects);
            // distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            // spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            // var groups = Methods.FindAcquaintanceGrouping.Find(trainSet, spheres, excludedObjects);
            // var standartObject = Methods.FindStandartObjects.Find(trainSet, groups, spheres, excludedObjects, distances, logger);
            // // logger.WriteLine("Result", $"Stability: {((trainSet.Objects.Length - noisyObjects.Count) / (decimal)trainSet.Objects.Length) * ((trainSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count)}");
            // logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            // // logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            // logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");

            // foreach (var st in standartObject)
            // {
            //     logger.WriteLine("Result", $"{{{st}, {distances.Radiuses[st]}M}}");
            // }

            // logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");
        }

        private static void TestManualFirstPair()
        {
            //
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - TestManualFirstPair");

            var trainSet = new Data.Train.ThrombinSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);

            var testObjects = new Data.Test.ThrombinTestSet().GetSet();
            System.Console.WriteLine($"Test objects count is {testObjects.ToString()}");

            // var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "TestManualFirstPair");

            // Finding all features weights
            var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            Parallel.For(0, trainSet.Features.Length, i =>
            {
                trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), trainSet.ClassValue);
            });


            for (int activeFeaturesCount = 1; activeFeaturesCount < 14; activeFeaturesCount++)
            {
                var activeFeatures = new List<int[]>();
                for (int i = 0; i < activeFeaturesCount; i++)
                {
                    using (var file = new StreamReader(Path.Combine("_Results", "20210518 154036 - Method2DByRS", $"0{i}. Set of features.txt.txt")))
                    {
                        var line = file.ReadLine();
                        activeFeatures.Add(Array.ConvertAll(line.Split(", ", StringSplitOptions.RemoveEmptyEntries), x => int.Parse(x, CultureInfo.InvariantCulture)));
                        System.Console.WriteLine(line);
                        System.Console.WriteLine(file.ReadLine());
                        System.Console.WriteLine("-");
                        System.Console.WriteLine($"{string.Join(", ", activeFeatures[i])}\n{activeFeatures[i].Length}");
                        System.Console.WriteLine("==============");
                    }
                }


                // var boundaries = new decimal[]{
                //     -7.5866635357567683545779447525M,
                //     -17.217427947597800497576543527M,
                //     -18.947083064615648402780217446M,
                //     -19.909148571440727100218398278M,
                //     -20.313326477449819516008280692M
                // };

                var rsList = new List<Dictionary<int, decimal>>();
                for (int i = 0; i < activeFeaturesCount; i++)
                    rsList.Add(Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), activeFeatures[i]));


                using (var resultFile = new StreamWriter($"Result file {DateTime.Now: yyyyMMdd HHmmss} {activeFeaturesCount}"))
                    foreach (var testObject in testObjects.Objects)
                    {
                        var classValue = new int?[activeFeaturesCount];
                        for (int i = 0; i < activeFeaturesCount; i++)
                        {
                            var rs = Methods.GeneralizedAssessment.FindNonContiniousFeature(testObject, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), activeFeatures[i]);
                            var min = new { Index = -1, Dist = -1M, ClassValue = (int?)null };
                            foreach (var obj in rsList[i])
                            {
                                var dist = Math.Abs(obj.Value - rs);
                                if (min.Index == -1 || min.Dist >= dist)
                                {
                                    if (min.Dist == dist && min.ClassValue.HasValue && trainSet.Objects[obj.Key].ClassValue != min.ClassValue)
                                    {
                                        min = new { Index = obj.Key, Dist = dist, ClassValue = (int?)null };
                                    }
                                    else
                                    {
                                        min = new { Index = obj.Key, Dist = dist, ClassValue = trainSet.Objects[obj.Key].ClassValue };
                                    }
                                }
                            }
                            classValue[i] = min.ClassValue;
                            // if (rs < boundaries[i])
                            // {
                            //     classValue[i] = 2;
                            // }
                            // else if (rs > boundaries[i])
                            // {
                            //     classValue[i] = 1;
                            // }
                            logger.WriteLine($"Test rs {i}", $"{rs}\t{classValue[i]}");
                        }

                        var k1 = classValue.Count(w => w == 1);
                        var k2 = classValue.Count(w => w == 2);

                        if (k1 != k2)
                        {
                            resultFile.WriteLine(k1 > k2 ? "A" : "I");
                        }
                        else
                        {
                            resultFile.WriteLine("N/A");
                        }
                    }
            }
        }

        private static void ManualFirstPair()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - ManualFirstPair");

            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "ManualFirstPair");

            // Finding all features weights
            var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            Parallel.For(0, trainSet.Features.Length, i =>
            {
                trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), trainSet.ClassValue);
            });


            var firstPair = new List<int>() { 8489, 12914 };
            var features = Methods.FindAllFeaturesByPhi.Find(trainSet, distFunc, logger, firstPair, trainSetFeatureWeights.OrderByDescending(o => o.Value.Value).Take(1500).Select(s => s.Key).ToHashSet());

            var excludedObjects = new HashSet<int>();

            var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            var spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            // var noisyObjects = Methods.FindNoisyObjects.Find(trainSet, spheres, excludedObjects, logger);
            // excludedObjects.UnionWith(noisyObjects);
            distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            var groups = Methods.FindAcquaintanceGrouping.Find(trainSet, spheres, excludedObjects);
            var standartObject = Methods.FindStandartObjects.Find(trainSet, groups, spheres, excludedObjects, distances, logger);
            // logger.WriteLine("Result", $"Stability: {((trainSet.Objects.Length - noisyObjects.Count) / (decimal)trainSet.Objects.Length) * ((trainSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count)}");
            logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            // logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");

            foreach (var st in standartObject)
            {
                logger.WriteLine("Result", $"{{{st}, {distances.Radiuses[st]}M}}");
            }

            logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");

        }

        private static void RS1000FindByDominance()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - RS1000FindByDominance");

            var rsSet = new Data.RS.ThrombinRS1000().GetSet();
            logger.WriteLine("Set info", rsSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(rsSet, "RS1000FindByDominance");

            var firstPair = Methods.FindFirstPairFeatureByDominance.Find(rsSet, distFunc, Enumerable.Range(0, rsSet.Features.Length), logger);

            var features = Methods.FindAllFeaturesByPhi.Find(rsSet, distFunc, logger, firstPair);

            var excludedObjects = new HashSet<int>();

            var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(rsSet, distFunc, features, excludedObjects);
            var spheres = Models.Sphere.FindAll(rsSet, distances, excludedObjects, true);
            var noisyObjects = Methods.FindNoisyObjects.Find(rsSet, spheres, excludedObjects, logger);
            excludedObjects.UnionWith(noisyObjects);
            distances = Utils.DistanceUtils.FindAllDistanceAndRadius(rsSet, distFunc, features, excludedObjects);
            spheres = Models.Sphere.FindAll(rsSet, distances, excludedObjects, true);
            var groups = Methods.FindAcquaintanceGrouping.Find(rsSet, spheres, excludedObjects);
            var standartObject = Methods.FindStandartObjects.Find(rsSet, groups, spheres, excludedObjects, distances, logger);
            logger.WriteLine("Result", $"Stability: {((rsSet.Objects.Length - noisyObjects.Count) / (decimal)rsSet.Objects.Length) * ((rsSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count)}");
            logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");

            logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");

        }

        private static void RS1000FindByRelativeCount()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - RS1000FindByRelativeCount");

            var rsSet = new Data.RS.ThrombinRS1000().GetSet();
            logger.WriteLine("01. Set info", rsSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(rsSet, "RS1000FindByRelativeCount");

            var firstPair = Methods.FindFirstPairFeatureByRelativesCount.Find(rsSet, distFunc, Enumerable.Range(0, rsSet.Features.Length), logger);

            var features = Methods.FindAllFeaturesByPhi.Find(rsSet, distFunc, logger, firstPair);
        }

        private static void FindingRSSet(int pageSize = 1000)
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - FindingRSSet {pageSize}");

            // Reading traind set
            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            logger.WriteLine("01. Set info", trainSet.ToString(), true);

            // Finding all features weights
            var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            Parallel.For(0, trainSet.Features.Length, i =>
            {
                trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(trainSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), trainSet.ClassValue);
            });

            var pages = new List<List<int>>();
            var pageIndex = 0;
            var counter = 0;
            foreach (var item in trainSetFeatureWeights.OrderByDescending(o => o.Value.Value))
            {
                if (pages.Count == pageIndex)
                    pages.Add(new List<int>());

                counter++;
                pages[pageIndex].Add(item.Key);

                if (counter % pageSize == 0)
                {
                    pageIndex++;
                }

                logger.WriteLine("02. Set feature weights", $"Feature[{item.Key}] {item.Value}", false);
            }

            // System.Console.WriteLine($"Pages count is : {pages.Count} with items ({pages.SelectMany(s => s).Count()})");

            var trainRS = new ConcurrentDictionary<int, Dictionary<int, decimal>>();

            Parallel.For(0, pages.Count, i =>
            {
                trainRS[i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(s => s.Key, s => s.Value), pages[i]);
            });

            for (int objectIndex = 0; objectIndex < trainSet.Objects.Length; objectIndex++)
            {
                logger.Write("03. RS set", $"{trainSet.Objects[objectIndex].ClassValue}");
                for (int i = 0; i < pages.Count; i++)
                {
                    logger.Write("03. RS set", $"\t{trainRS[i][objectIndex]}");
                }
                logger.WriteLine("03. RS set", "");
            }

            var rsWeights = new ConcurrentDictionary<int, Criterions.FirstCriterion.FirstCriterionResult>();


            Parallel.For(0, pages.Count, i =>
            {
                rsWeights[i] = Criterions.FirstCriterion.Find(trainRS[i].Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = trainSet.Objects[s.Key].ClassValue.Value,
                    Distance = s.Value,
                    ObjectIndex = s.Key
                }), trainSet.ClassValue);
            });

            foreach (var item in rsWeights.OrderByDescending(o => o.Value.Value))
            {
                logger.WriteLine("04. RS weights ordered", $"Feature {item.Key:00}\t Weight = {item.Value.Value}");
            }
        }

        private static void OldMethod()
        {
            var uniqueFeatureIndexList = new List<int>();

            using (var file = new StreamReader(Path.Combine("Data", "Train", "thrombin_unique_features_indexes.data")))
            {
                while (!file.EndOfStream)
                {
                    uniqueFeatureIndexList.Add(Convert.ToInt32(file.ReadLine()));
                }
            }

            var uniqueObjectIndexList = new List<int>();
            using (var file = new StreamReader(Path.Combine("Data", "Train", "thrombin_unique_set_True.data.indexes")))
            {
                while (!file.EndOfStream)
                {
                    uniqueObjectIndexList.Add(Convert.ToInt32(file.ReadLine()));
                }
            }

            // var uniqueSet = new Data.Train.ThrombinUniqueSet().GetSet();

            var uniqueObjects = new List<Models.ObjectInfo>();
            int objIndex = 0, lineIndex = -1;
            using (var file = new StreamReader(Path.Combine("Data", "Train", "thrombin.data")))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    lineIndex++;
                    if (!uniqueObjectIndexList.Contains(lineIndex))
                        continue;
                    var cl = line[0];
                    var data = new decimal[uniqueFeatureIndexList.Count];
                    int i = 0;
                    foreach (var ft in uniqueFeatureIndexList)
                    {
                        data[i++] = (line[ft * 2] == '0') ? 0 : 1;
                    }
                    uniqueObjects.Add(new Models.ObjectInfo()
                    {
                        Data = data,
                        ClassValue = cl == 'A' ? 1 : 2,
                        Index = objIndex++
                    });
                }
            }
            var uniqueSet = new Models.ObjectSet("Thrombin unique set", uniqueObjects.ToArray(), uniqueFeatureIndexList.Select(s => new Models.Feature { IsContinuous = false, Name = $"Ft {s:0000000}" }).ToArray());

            System.Console.WriteLine(uniqueSet);

            var testObjects = new List<Models.ObjectInfo>();
            using (var testFile = new StreamReader(Path.Combine("Data", "Test", "Thrombin.testset")))
            {
                while (!testFile.EndOfStream)
                {
                    var line = testFile.ReadLine();
                    var data = new decimal[uniqueFeatureIndexList.Count];
                    for (int i = 0; i < uniqueFeatureIndexList.Count; i++)
                    {
                        var ft = uniqueFeatureIndexList[i];
                        if (line[ft * 2] == '0')
                            data[i] = 0;
                        else if (line[ft * 2] == '1')
                            data[i] = 1;
                        else
                        {
                            System.Console.WriteLine("Cannot read normally test objects.");
                            return;
                        }
                    }
                    testObjects.Add(new Models.ObjectInfo()
                    {
                        Data = data
                    });
                }
            }
            System.Console.WriteLine($"Test objects count is {testObjects.Count}");


            var uniqueFeatureWeights = new Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            for (int i = 0; i < uniqueSet.Features.Length; i++)
            {
                uniqueFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(uniqueSet.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }), uniqueSet.ClassValue);
            }
            System.Console.WriteLine($"Unique features count is {uniqueFeatureWeights.Count}");

            var standartObjects = new List<Models.ObjectInfo>();
            var standartObjectIndexes = new int[] { 96, 308, 413, 495, 508, 584, 908 };

            var informativeFeatures = new int[] { 45, 63, 64, 90 };

            var radius = new Dictionary<int, decimal>{
                {308, 0.066058322487741M}
                ,{584, 0.0686661101100827M}
                ,{508, 0.0800644979288151M}
                ,{495, 0.108528542080056M}
                ,{413, 0.084041460003191M}
                ,{908, 2.57272285192743M}
                ,{96, 0.0208319748669761M}
            };

            // var informativeFeatures = new int[909];
            // for (int i = 1; i <= 909; i++)
            // {
            //     informativeFeatures[i - 1] = i;
            // }

            int objectIndex = -1;
            var pageSize = 1000;
            using (var file = new StreamReader(Path.Combine("Data", "RS", $"thrombin_rs_set_{pageSize}.txt")))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    if (!standartObjectIndexes.Contains(++objectIndex))
                        continue;
                    var data = new decimal[informativeFeatures.Length];
                    var dt = line.Substring(2).Split('\t');
                    int dataIndex = 0;
                    foreach (var ft in informativeFeatures)
                    {
                        data[dataIndex++] = decimal.Parse(dt[ft], CultureInfo.InvariantCulture);
                    }
                    standartObjects.Add(new Models.ObjectInfo()
                    {
                        Index = objectIndex,
                        ClassValue = (line[0] == '1' ? 1 : 2),
                        Data = data
                    });
                }
            }
            System.Console.WriteLine($"Standart objects count is {standartObjects.Count}");


            var featureRs = new Models.Feature[informativeFeatures.Length];
            var activeFeatures = new int[informativeFeatures.Length];
            for (int i = 0; i < informativeFeatures.Length; i++)
            {
                activeFeatures[i] = i;
                featureRs[i] = new Models.Feature
                {
                    IsContinuous = true,
                    Name = "Rs ft " + informativeFeatures[i]
                };
            }

            var uniqueFeatureWeightsOrdered = uniqueFeatureWeights.OrderByDescending(o => o.Value.Value).Select(s => s.Key).ToList();
            using (var file = new StreamWriter("unique feature weights.txt"))
            {
                foreach (var ft in uniqueFeatureWeightsOrdered)
                {
                    file.WriteLine($"{ft}\t{uniqueFeatureWeights[ft].Value:0.000000}");
                }
            }

            var informativeFeaturesWeights = new Dictionary<int, Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>>();
            foreach (var ft in informativeFeatures)
            {
                informativeFeaturesWeights[ft] = new Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
                for (int i = 0; i < pageSize; i++)
                {
                    if (uniqueFeatureWeightsOrdered.Count > i + ft * pageSize)
                    {
                        informativeFeaturesWeights[ft][uniqueFeatureWeightsOrdered[i + ft * pageSize]] = uniqueFeatureWeights[uniqueFeatureWeightsOrdered[i + ft * pageSize]];
                    }
                }
            }
            // using (var file = new StreamWriter("portion.txt"))
            //     foreach (var item in informativeFeaturesWeights)
            //     {
            //         file.WriteLine($"Feature {item.Key}");

            //         foreach (var ft in item.Value)
            //         {
            //             file.WriteLine($"{ft.Key} \t {ft.Value.Value:0.000000}");
            //         }
            //     }
            using (var resultFile = new StreamWriter($"RS file {DateTime.Now: ddMMyyyy HH mm ss}"))
            {
                foreach (var objInd in standartObjectIndexes)
                {
                    resultFile.Write($"{objInd}\t{uniqueSet.Objects[objInd].ClassValue}\t");
                    foreach (var ft in informativeFeatures)
                    {
                        var rs = Methods.GeneralizedAssessment.FindNonContiniousFeature(uniqueSet.Objects[objInd], informativeFeaturesWeights[ft], informativeFeaturesWeights[ft].Select(s => s.Key));
                        resultFile.Write($"{rs:0.000000}\t");
                    }
                    resultFile.WriteLine();
                }

                foreach (var item in standartObjects)
                {
                    resultFile.WriteLine(item);
                }
            }

            using (var resultFile = new StreamWriter($"Result file {DateTime.Now: yyyyMMdd HHmmss}"))
                foreach (var testObject in testObjects)
                {
                    var testRs = new Models.ObjectInfo();
                    testRs.Data = new decimal[informativeFeatures.Length];
                    int ind = 0;
                    foreach (var ft in informativeFeatures.OrderBy(o => o))
                    {
                        testRs[ind++] = Methods.GeneralizedAssessment.FindNonContiniousFeature(testObject, informativeFeaturesWeights[ft], informativeFeaturesWeights[ft].Select(s => s.Key));
                    }
                    decimal? minDist = null;
                    int? classValue = null;
                    foreach (var standartObject in standartObjects)
                    {
                        var dist = new Metrics.Euclidean().Calculate(testRs, standartObject, featureRs, activeFeatures) / radius[standartObject.Index];
                        if (minDist == null || minDist > dist)
                        {
                            classValue = standartObject.ClassValue;
                            minDist = dist;
                        }
                        else if (minDist == dist && classValue != standartObject.ClassValue)
                        {
                            classValue = null;
                        }
                    }
                    if (classValue != null)
                    {
                        resultFile.WriteLine(classValue == 1 ? "A" : "I");
                    }
                    else
                    {
                        resultFile.WriteLine("N/A");
                    }
                }
        }
    }
}
