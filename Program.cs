using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var set = Models.ObjectSet.FromFileData("Data/Train/Dry_Bean.txt");

            System.Console.WriteLine(set);

            System.Console.WriteLine("Normalizing data set...");
            set = Methods.NormilizingMinMax.Normalize(set);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(set, "For distance");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            System.Console.WriteLine($"Finding all distances at {DateTime.Now}...");
            var dist = Utils.DistanceUtils.FindAllDistance(set, distFunc, Enumerable.Range(0, set.Features.Length), false);

            System.Console.WriteLine($"Finding all spheres at {DateTime.Now}...");
            var spheres = Models.Sphere.FindAll(set, dist, null, true);
            System.Console.WriteLine($"Founded {spheres.Count()} spheres...");

            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - FindNoisyObjects");

            logger.WriteLine("Spheres with noisy objects", string.Join(Environment.NewLine, spheres.OrderBy(o => o.ObjectIndex)));

            logger.WriteLine("Boundary objects with noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Enemies).Distinct().OrderBy(o => o)));

            logger.WriteLine("Coverage objects with noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Coverage).Distinct().OrderBy(o => o)));

            var noisyObjects = Methods.FindNoisyObjects.Find(set, spheres, logger);
            System.Console.WriteLine($"Noisy objects ({noisyObjects.Count}){{{string.Join(", ", noisyObjects)}}}");

            System.Console.WriteLine($"Finding all spheres at {DateTime.Now}...");
            spheres = Models.Sphere.FindAll(set, dist, noisyObjects, true);
            System.Console.WriteLine($"Founded {spheres.Count()} spheres...");

            logger.WriteLine("Spheres without noisy objects", string.Join(Environment.NewLine, spheres.OrderBy(o => o.ObjectIndex)));

            logger.WriteLine("Boundary objects without noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Enemies).Distinct().OrderBy(o => o)));

            logger.WriteLine("Coverage objects without noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Coverage).Distinct().OrderBy(o => o)));

            var groups = Methods.FindAcquaintanceGrouping.Find(set, spheres);
            System.Console.WriteLine($"New groups count {groups.Count}");

            foreach (var group in groups.OrderByDescending(o => o.Count))
            {
                logger.WriteLine("Groups", $"Group ({group.Count}) {{{string.Join(", ", group.OrderBy(o => o))}}}");
            }

            var standartObject = Methods.FindStandartObjects.Find(set, groups, spheres, dist, logger);

            // System.Console.WriteLine($"Standart object ({standartObject.Count}) {{{string.Join(", ", standartObject)}}}");


            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            // Method2DByRS();

            // while (true)
            // {
            //     System.Console.WriteLine("Menu:");
            //     System.Console.WriteLine("\t1. Finding new RS set");
            //     System.Console.WriteLine("\t2. kNN checking");
            //     System.Console.WriteLine("\t0. Exit");
            //     System.Console.Write("Type number (default 1):");
            //     var ans = Console.ReadLine();
            //     int a = 1;
            //     if (!int.TryParse(ans, out a))
            //         a = 1;
            //     switch (a)
            //     {
            //         case 0: return;
            //         case 1:
            //             System.Console.Write("Page size (default 1000):");
            //             var p = Console.ReadLine();
            //             var pageSize = 1000;
            //             if (!int.TryParse(p, out pageSize))
            //                 pageSize = 1000;
            //             FindingRSSet(pageSize);
            //             break;
            //         case 2:
            //             CheckKNN();
            //             break;
            //     }
            // }

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
            System.Console.WriteLine($"Done at {DateTime.Now}...");
            // Console.ReadKey();
        }

        private static void Method2DByRS()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");
            var testSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects();
            var trainSet = new Data.Test.ThrombinTestSet().GetSet(); //

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

            var newFeaturesCount = 20;
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

        private static void FindingRSSet(int pageSize = 1000)
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - FindingRSSet {pageSize}");

            System.Console.WriteLine("Reading sets...");
            // Reading train set
            var testSet = new Data.Test.ThrombinTestSet().GetSet();
            logger.WriteLine("01. Set info", testSet.ToString(), true);

            //Reading test set
            var trainSet = new Data.Train.ThrombinSet().GetSetUniqueByObjects();
            logger.WriteLine("01. Set info", trainSet.ToString(), true);

            System.Console.WriteLine("Finding all features weights...");
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

            System.Console.WriteLine("Order feature weights value by descending...");
            var orderedWeights = trainSetFeatureWeights.OrderByDescending(o => o.Value.Value).ToDictionary(k => k.Key, v => v.Value);

            foreach (var item in orderedWeights)
            {
                logger.WriteLine("02. Ordered feature weights", $"{item.Key:0000}. {item.Value}");
            }

            int totalPages = (int)Math.Ceiling(orderedWeights.Count / (decimal)pageSize);

            System.Console.WriteLine("Finding RS (Generalized assessment value)...");
            var trainRsData = Enumerable.Range(0, trainSet.Objects.Length).Select(s => new Models.ObjectInfo
            {
                ClassValue = trainSet.Objects[s].ClassValue,
                Data = new decimal[totalPages],
                Index = s
            }).ToArray();
            var testRsData = Enumerable.Range(0, testSet.Objects.Length).Select(s => new Models.ObjectInfo
            {
                ClassValue = trainSet.Objects[s].ClassValue,
                Data = new decimal[totalPages],
                Index = s
            }).ToArray();

            for (int i = 0; i < totalPages; i++)
            {
                var activeFeatures = orderedWeights.Skip(i * pageSize).Take(pageSize).Select(s => s.Key).ToArray();
                for (int j = 0; j < trainSet.Objects.Length; j++)
                {
                    trainRsData[j][i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet.Objects[j], orderedWeights, activeFeatures);
                }
                for (int j = 0; j < testSet.Objects.Length; j++)
                {
                    testRsData[j][i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(testSet.Objects[j], orderedWeights, activeFeatures);
                }
            }

            System.Console.WriteLine("Writing result to files...");
            var trainRsSet = new Models.ObjectSet($"Thrombin train RS{pageSize}", trainRsData, Enumerable.Range(0, totalPages).Select(s => new Models.Feature
            {
                IsContinuous = true,
                Name = $"RS {s:00000}"
            }).ToArray());
            trainRsSet.ToFileData(Path.Combine("Data", "RS", $"{trainRsSet.Name}.txt"));

            var testRsSet = new Models.ObjectSet($"Thrombin test RS{pageSize}", testRsData, Enumerable.Range(0, totalPages).Select(s => new Models.Feature
            {
                IsContinuous = true,
                Name = $"RS {s:00000}"
            }).ToArray());
            testRsSet.ToFileData(Path.Combine("Data", "RS", $"{testRsSet.Name}.txt"));

            System.Console.WriteLine("Files:");
            System.Console.WriteLine($"\t{Path.Combine("Data", "RS", $"{trainRsSet.Name}.txt")}");
            System.Console.WriteLine($"\t{Path.Combine("Data", "RS", $"{testRsSet.Name}.txt")}");
            System.Console.WriteLine("Done.");
        }

        private static void CheckKNN()
        {
            var dir = new DirectoryInfo(Path.Combine("Data", "RS"));
            var allFiles = dir.GetFiles("*.txt");
            int trainFileNumber;
            do
            {
                System.Console.WriteLine("Choose train set:");
                for (int i = 0; i < allFiles.Length; i++)
                {
                    System.Console.WriteLine($"\t{i}. {allFiles[i].Name}");
                }
                System.Console.Write("Type number:");
            } while (!int.TryParse(Console.ReadLine(), out trainFileNumber));

            var trainSet = Models.ObjectSet.FromFileData(allFiles[trainFileNumber].FullName);

            int testFileNumber;
            do
            {
                System.Console.WriteLine("Choose test set:");
                for (int i = 0; i < allFiles.Length; i++)
                {
                    System.Console.WriteLine($"\t{i}. {allFiles[i].Name}");
                }
                System.Console.Write("Type number:");
            } while (!int.TryParse(Console.ReadLine(), out testFileNumber));

            var testSet = Models.ObjectSet.FromFileData(allFiles[testFileNumber].FullName);

            System.Console.WriteLine("Norming");
        }
    }
}
