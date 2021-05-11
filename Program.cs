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
            Method2DByRS();
        }

        private static void Method2DByRS()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");

            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            // var data = new Models.ObjectInfo[149];
            // var ind = 0;
            // using (var f = new StreamReader(Path.Combine("Data", "Train", "tubnb.dat")))
            // {
            //     while (!f.EndOfStream)
            //     {
            //         var line = f.ReadLine().Trim();
            //         System.Console.WriteLine(line);

            //         var d = Array.ConvertAll(line.Split(' ', StringSplitOptions.RemoveEmptyEntries), x => decimal.Parse(x, CultureInfo.InvariantCulture));
            //         data[ind] = new Models.ObjectInfo
            //         {
            //             Data = d.Take(d.Length - 1).ToArray(),
            //             ClassValue = (int)d[d.Length - 1],
            //             Index = ind++,
            //         };
            //         System.Console.WriteLine($"{d.Length}\n{string.Join(" ", d.Take(d.Length - 1))}\n\n");
            //     }
            // }
            // var trainSet = new Models.ObjectSet("Tuber", data, Enumerable.Range(0, 48).Select(s => new Models.Feature { IsContinuous = false, Name = $"Ft {s:00}" }).ToArray(), 1);

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

            var orderedFeatures = trainSetFeatureWeights.OrderByDescending(o => o.Value.Value).Take(1500).Select(s => s.Key).ToList();

            var newFeaturesCount = 5;
            var newFeatures = new Models.Feature[newFeaturesCount];

            var rs = new Dictionary<int, Dictionary<int, decimal>>();
            var boundary = new Dictionary<int, decimal>();
            for (int i = 0; i < newFeaturesCount; i++)
            {
                newFeatures[i] = new Models.Feature { Name = $"Ft of RS {i}", IsContinuous = true };
                var firstPair = new List<int>() { orderedFeatures[0] };
                var featuresSet = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
                logger.WriteLine($"0{i}. Set of features.txt", string.Join(", ", featuresSet));

                rs[i] = Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), featuresSet);

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



            var rsSet = new Models.ObjectSet("Method3DByRs set", rsObjects, newFeatures, 1);
            rsSet.ToFileData("new rs set");
            var excludedObjects = new HashSet<int>();

            trainSet = rsSet;
            var features = Enumerable.Range(0, newFeaturesCount);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method3DByRS set");
            var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            var spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            var noisyObjects = Methods.FindNoisyObjects.Find(trainSet, spheres, excludedObjects, logger);
            excludedObjects.UnionWith(noisyObjects);
            distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            spheres = Models.Sphere.FindAll(trainSet, distances, excludedObjects, true);
            var groups = Methods.FindAcquaintanceGrouping.Find(trainSet, spheres, excludedObjects);
            var standartObject = Methods.FindStandartObjects.Find(trainSet, groups, spheres, excludedObjects, distances, logger);
            logger.WriteLine("Result", $"Stability: {((trainSet.Objects.Length - noisyObjects.Count) / (decimal)trainSet.Objects.Length) * ((trainSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count)}");
            logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");

            foreach (var st in standartObject)
            {
                logger.WriteLine("Result", $"{{{st}, {distances.Radiuses[st]}M}}");
            }

            logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");
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

            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "TestManualFirstPair");

            var uniqueFeatureIndexList = new List<int>();
            using (var file = new StreamReader(Path.Combine("Data", "Train", "thrombin_unique_features_indexes.data")))
            {
                while (!file.EndOfStream)
                {
                    uniqueFeatureIndexList.Add(Convert.ToInt32(file.ReadLine()));
                }
            }

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

            var activeFeatures = new int[] { 8489, 12832, 57897, 10483, 21211, 19596, 57860, 64276, 20150, 65195, 57608, 88858, 20265, 52110, 23039, 2638, 20036, 12836, 20028, 17613, 65075, 57594, 21084, 20262, 19613, 64226, 58236, 22436, 88898, 8747, 57639, 15824, 2408, 60381, 13288, 19975, 63089, 19405, 10299, 20635, 27477, 20142, 8465, 19173, 63828, 39192, 21025, 48105, 19751, 9117, 11125, 19777, 42389, 20972, 18517, 64271, 60539, 63471, 57863, 20872, 20133, 66127, 9439, 19414, 18912, 20205, 13016, 21321, 19416, 23075, 2428, 65625, 19401, 8641, 74233, 13284, 66000, 24666, 17422, 8363, 19089, 12864, 57681, 21414, 68086, 24887 };

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

            using (var resultFile = new StreamWriter($"Result file {DateTime.Now: yyyyMMdd HHmmss}"))
                foreach (var testObject in testObjects)
                {
                    decimal? minDist = null;
                    int? classValue = null;
                    var rs = Methods.GeneralizedAssessment.FindNonContiniousFeature(testObject, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), activeFeatures);

                    if (rs < -8.261894912292053306428601170M)
                    {
                        classValue = 2;
                    }
                    else if (rs > -8.261894912292053306428601170M)
                    {
                        classValue = 1;
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
