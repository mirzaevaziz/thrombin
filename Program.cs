using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            TestManualFirstPair();
        }

        private static void TestManualFirstPair()
        {
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

            var activeFeatures = new int[] { 2408, 2638, 8041, 8392, 8402, 8404, 8459, 8489, 8596, 8597, 8612, 8747, 8752, 12799, 12914, 13288, 15076, 15824, 17422, 18829, 18912, 19325, 19401, 19777, 19975, 20838, 20943, 21084, 21301, 21321, 21551, 24500, 24556, 24573, 24582, 24638, 24666, 24760, 24774, 24816, 24877, 24880, 24887, 24891, 41452, 44027, 48444, 57866, 62207, 63089, 63183, 63268, 64226, 65075, 65195, 66112, 66127, 66313 };
            var standartObjects = new int[] { 83, 106, 308, 349, 357, 406, 414, 508, 519, 570, 607, 662, 668, 696, 712, 735, 740, 882, 911, 938, 947, 1061, 1097, 1098, 1099, 1238, 1242, 1275, 1296 };
            var standartObjectsRadius = new Dictionary<int, decimal>{
                    {1275, 2M}
                    ,{83, 4M}
                    ,{508, 8M}
                    ,{668, 9M}
                    ,{712, 12M}
                    ,{740, 12M}
                    ,{570, 13M}
                    ,{1098, 15M}
                    ,{607, 16M}
                    ,{947, 3M}
                    ,{519, 6M}
                    ,{911, 27M}
                    ,{1061, 34M}
                    ,{106, 7M}
                    ,{1097, 8M}
                    ,{662, 2M}
                    ,{1099, 4M}
                    ,{882, 1M}
                    ,{696, 1M}
                    ,{938, 8M}
                    ,{735, 4M}
                    ,{308, 14M}
                    ,{349, 1M}
                    ,{357, 1M}
                    ,{1238, 9M}
                    ,{406, 5M}
                    ,{414, 8M}
                    ,{1242, 9M}
                    ,{1296, 1M}
            };

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
                    foreach (var standartObject in standartObjects)
                    {
                        var dist = distFunc(testObject, trainSet.Objects[standartObject], trainSet.Features, activeFeatures) / standartObjectsRadius[standartObject];
                        if (minDist == null || minDist > dist)
                        {
                            classValue = trainSet.Objects[standartObject].ClassValue;
                            minDist = dist;
                        }
                        else if (minDist == dist && classValue != trainSet.Objects[standartObject].ClassValue)
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
                    line = line.Substring(2);
                    var data = new decimal[uniqueFeatureIndexList.Count];
                    int i = 0;
                    foreach (var ft in uniqueFeatureIndexList)
                    {
                        data[i++] = (line[(ft - 1) * 2] == '0') ? 0 : 1;
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
                        var rs = Methods.GeneralizedAssessment.FindNonContiniousFeature(uniqueSet.Objects[objInd], informativeFeaturesWeights[ft]);
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
                        testRs[ind++] = Methods.GeneralizedAssessment.FindNonContiniousFeature(testObject, informativeFeaturesWeights[ft]);
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
