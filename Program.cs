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
            // TestManualFirstPair();
            // Method2D();
            Method2DByRS();
        }

        private static void Method2DByRS()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");

            var trainSet = new Data.Train.ThrombinUniqueSet().GetSet();
            logger.WriteLine("Set info", trainSet.ToString(), true);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method2DByRS");

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
            // var features1 = new int[] { 8489, 12832, 57897, 10483, 21211, 19596, 57860, 64276, 20150, 65195, 57608, 88858, 20265, 52110, 23039, 2638, 20036, 12836, 20028, 17613, 65075, 57594, 21084, 20262, 19613, 64226, 58236, 22436, 88898, 8747, 57639, 15824, 2408, 60381, 13288, 19975, 63089, 19405, 10299, 20635, 27477, 20142, 8465, 19173, 63828, 39192, 21025, 48105, 19751, 9117, 11125, 19777, 42389, 20972, 18517, 64271, 60539, 63471, 57863, 20872, 20133, 66127, 9439, 19414, 18912, 20205, 13016, 21321, 19416, 23075, 2428, 65625, 19401, 8641, 74233, 13284, 66000, 24666, 17422, 8363, 19089, 12864, 57681, 21414, 68086, 24887 };
            var features1 = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
            logger.WriteLine("01. First set of features.txt", string.Join(", ", features1));
            var crit1Result = Criterions.FirstCriterion.Find(Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(s => s.Key, s => s.Value), features1).Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
            {
                ClassValue = trainSet.Objects[s.Key].ClassValue.Value,
                Distance = s.Value,
                ObjectIndex = s.Key
            }), trainSet.ClassValue);

            logger.WriteLine("01. First set of features.txt", $"Feature count is {features1.Count()}\nCriterion1 result is {crit1Result}");

            orderedFeatures = orderedFeatures.Except(features1).ToList();
            firstPair = new List<int>() { orderedFeatures[0] };
            // var features2 = new int[] { 12914, 8519, 26772, 26934, 16007, 20143, 57696, 2592, 19614, 22562, 39181, 16057, 2839, 22564, 19599, 21368, 22391, 52093, 20983, 20146, 57935, 19770, 64143, 42443, 8600, 65193, 73837, 25679, 12515, 17511, 20245, 16246, 22612, 57951, 27422, 66119, 18088, 20282, 2384, 24753, 13440, 52114, 17735, 20243, 16760, 21301, 24880, 65057, 57612, 8041, 22294, 22366, 20930, 22624, 52191, 19459, 48980, 20856, 9002, 22680, 57972, 15821, 64100, 60363, 16422, 20033, 7970, 57697, 5712, 21262, 67458, 12799, 12395, 66112, 44370, 8262, 22665, 19385, 64731, 64398, 57939, 758, 58028, 8752, 65263, 2194, 20020, 8612, 66084, 85895, 17582, 18392, 19325, 19028, 12720, 18934, 9204, 16580, 20135, 27617, 57866, 17579, 8772, 22744, 41452, 21068, 19571, 24556, 13236, 19402, 11128, 17682, 22434, 24821, 41700, 18944, 24943, 65042, 8278, 21433, 20838, 57691, 13080, 64195, 42432, 19468, 13448, 57642, 64761, 19540, 9291, 57766, 20238, 8629 };
            var features2 = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());

            logger.WriteLine("02. Second set of features.txt", string.Join(", ", features2));
            crit1Result = Criterions.FirstCriterion.Find(Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(s => s.Key, s => s.Value), features2).Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
            {
                ClassValue = trainSet.Objects[s.Key].ClassValue.Value,
                Distance = s.Value,
                ObjectIndex = s.Key
            }), trainSet.ClassValue);

            logger.WriteLine("02. Second set of features.txt", $"Feature count is {features2.Count()}\nCriterion1 result is {crit1Result}");

            orderedFeatures = orderedFeatures.Except(features2).ToList();
            firstPair = new List<int>() { orderedFeatures[0] };
            // var features3 = new int[] { 26745, 8597, 26716, 12876, 12540, 67367, 57725, 12676, 9147, 19101, 8392, 39393, 8611, 8461, 13794, 10546, 8303, 57599, 22678, 13303, 18877, 12712, 24767, 58744, 19517, 24891, 50215, 52124, 19610, 39475, 25909, 66116, 44492, 19766, 60217, 805, 22674, 39546, 21551, 57679, 8440, 25923, 44268, 20836, 19343, 65726, 16241, 19398, 19453, 24717, 19521, 57623, 64377, 13307, 58615, 48257, 20029, 13745, 19673, 2634, 64369, 22457, 8989, 17704, 8459, 21412, 13346, 64201, 24872, 21411, 58627, 63268, 64372, 18092, 19087, 24774, 86259, 25653, 12834, 19690, 58152, 23555, 24602, 66313, 23582, 24816, 62207, 41622, 67986, 2664, 18119, 8404, 18705, 24857, 52090, 42441, 82974, 10100, 20978, 19103, 12568, 67216, 9373, 13268, 52047, 8607, 64355, 18106, 12468, 57844, 67473, 22572, 27273, 2833, 22308, 63183, 11342, 12545, 19018, 64352, 20815, 58125, 2586, 19098, 20888, 57847, 14033, 18154, 50320, 20745, 19707, 18269, 19616, 8575, 20993, 26862 };
            var features3 = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());

            logger.WriteLine("03. Third set of features.txt", string.Join(", ", features3));
            crit1Result = Criterions.FirstCriterion.Find(Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(s => s.Key, s => s.Value), features3).Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
            {
                ClassValue = trainSet.Objects[s.Key].ClassValue.Value,
                Distance = s.Value,
                ObjectIndex = s.Key
            }), trainSet.ClassValue);

            logger.WriteLine("03. Third set of features.txt", $"Feature count is {features3.Count()}\nCriterion1 result is {crit1Result}");

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
