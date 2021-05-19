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
            // FindSimilarObjects();
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

        private static void Method2DByRS()
        {
            var logger = new Helpers.Logger($"{DateTime.Now:yyyyMMdd HHmmss} - Method2DByRS");

            var trainSet = new Data.Train.ThrombinSet().GetSet();
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

            var newFeaturesCount = 13;
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
            // var rsSet = new Data.RS.ThrombinRS5().GetSet();

            for (int i = 0; i < newFeaturesCount; i++)
            {
                var crit1Result = Criterions.FirstCriterion.Find(rsSet.Objects.Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    Distance = s[i],
                    ObjectIndex = s.Index
                }), rsSet.ClassValue);

                logger.WriteLine("Criterion 1 results", crit1Result.ToString());
                logger.WriteLine("Criterion 1 results", $"\tMin: {rsSet.Objects.Min(m => m[i])}, Max: {rsSet.Objects.Max(m => m[i])}");

                logger.WriteLine("Criterion 1 results", $"\tUnique values: {rsSet.Objects.Select(m => m[i]).Distinct().Count()}");

                var near = rsSet.Objects.Where(w => w[i] > crit1Result.Distance).Min(m => m[i]);
                logger.WriteLine("Criterion 1 results", $"\tNear: {near}, Boundary: {(crit1Result.Distance + near) / 2M}");

                logger.WriteLine("Criterion 1 results", $"\tK1: {rsSet.Objects.Where(w => w[i] > near && w.ClassValue == 1).Count()}");
                logger.WriteLine("Criterion 1 results", $"\tK2: {rsSet.Objects.Where(w => w[i] < near && w.ClassValue != 1).Count() + rsSet.Objects.Where(w => w[i] < near && w.ClassValue != 1).Count()}");
                logger.WriteLine("Criterion 1 results", $"\tK2: {((rsSet.Objects.Where(w => w[i] > near && w.ClassValue == 1).Count() + rsSet.Objects.Where(w => w[i] < near && w.ClassValue != 1).Count()) / (decimal)rsSet.Objects.Length) * 100}%");
            }

            rsSet.ToFileData("new rs set");
            rsSet = Methods.NormilizingMinMax.Normalize(rsSet);
            var excludedObjects = new HashSet<int>();

            trainSet = rsSet;
            var features = Enumerable.Range(0, newFeaturesCount);

            var distFunc = Metrics.MetricFunctionGetter.GetMetric(trainSet, "Method3DByRS set");
            var distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            var spheres = Models.Sphere.FindAll(trainSet, distances.Distances, excludedObjects, true);
            var noisyObjects = Methods.FindNoisyObjects.Find(trainSet, spheres, excludedObjects, logger);
            excludedObjects.UnionWith(noisyObjects);
            distances = Utils.DistanceUtils.FindAllDistanceAndRadius(trainSet, distFunc, features, excludedObjects);
            spheres = Models.Sphere.FindAll(trainSet, distances.Distances, excludedObjects, true);
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

            var activeFeatures = new List<int[]>{
new int[] { 10694, 16793, 79650, 13358, 29152, 26237, 90405, 79603, 27150, 91838, 79244, 135816, 27359, 32596, 79419, 3391, 72033, 16797, 27355, 23405, 26964, 90587, 89728, 79225, 24797, 28851, 26264, 100530, 31643, 90330, 26952, 26871, 28531, 83119, 113059, 88024, 100784, 25987, 35281, 135881, 79290, 88790, 92747, 10890, 90494, 54841, 11642, 29422, 17358, 26448, 29530, 24759, 39498, 27135, 12068, 29311, 136005, 27122, 83344, 3157, 79608, 32637, 92646, 14225, 26006, 88308, 23595, 93357, 29289, 16829, 79278, 91878, 25497, 17003, 135964, 106988, 35433, 28681, 79696, 35544
 },
 new int[] { 16886, 38574, 10728, 38767, 20973, 25285, 27136, 3340, 79378, 79236, 26265, 21032, 79425, 31819, 3627, 54826, 28636, 31817, 96146, 79656, 35716, 31580, 29368, 90361, 27141, 26242, 72008, 90185, 59736, 26471, 79463, 38337, 28801, 31913, 39441, 31898, 27319, 21279, 106428, 27383, 16370, 17584, 91835, 37102, 72038, 136076, 93541, 23289, 22041, 91649, 17354, 25960, 91624, 79361, 79739, 31989, 31546, 35545, 26080, 29224, 28601, 31434, 83098, 24171, 28511, 72150, 10084, 135193, 31969, 7299, 26957, 20724, 91930, 79382, 21511, 79718, 79615, 96344, 27317, 16192, 9986, 79248, 26944, 90598, 93528, 93483, 2888, 35708, 23365, 911, 113055, 79701, 115390, 99035, 67858, 91119, 39664, 25303, 21785, 79825, 72013, 90094, 26172, 62373, 11471, 27125, 3108, 130623, 28816, 11041, 17281, 58406, 26204, 28580, 25317, 91605, 10395, 29446, 26089, 32100, 35274, 93688, 25418, 58714, 25983, 11749, 24567, 29621, 17097, 90269, 31641, 26146, 135985, 124017, 16639, 79234, 79468, 11868, 79373, 62524, 28480, 10825, 91152, 35640, 90557, 90560
 },
 new int[] { 38543, 10821, 38509, 16846, 16403, 25064, 96181, 79420, 16591, 11678, 25514, 10548, 37135, 55116, 26209, 10660, 24038, 79651, 79823, 79261, 29443, 13427, 18091, 35566, 10843, 93531, 10446, 38423, 80817, 16630, 25234, 36609, 26467, 37402, 55215, 964, 97591, 26261, 69500, 35721, 79293, 10634, 93538, 17382, 17599, 31983, 17377, 25912, 82920, 26150, 59721, 35497, 37423, 92793, 28477, 28696, 62256, 21271, 26073, 23553, 72056, 35635, 11077, 27308, 29419, 55328, 18032, 31683, 35794, 10874, 79359, 35569, 11456, 26380, 25979, 24175, 35329, 26953, 88388, 90567, 26361, 124031, 33438, 29420, 33406, 17435, 3420, 91743, 80644, 10416, 37074, 25017, 35700, 10658, 24208, 69194, 90275, 94100, 14230, 31830, 10839, 35682, 10568, 28675, 92878, 3387, 79982, 86221, 88231, 131331, 16440, 39257, 16795, 11966, 66941, 80659, 90334, 25517, 12882, 28442, 97592, 16315, 25495, 3331, 123834, 28358, 27123, 96361, 35187, 79587, 99391, 79402, 3620, 79583, 59734, 16408, 14511, 35505, 95960, 24409, 72004, 26397, 38681, 23954, 58617, 17324, 26269, 35452, 11624, 79909, 90539, 3857, 71951, 16445, 16835, 38760, 25407, 28427, 31451, 10772, 35538
 },
 new int[] { 36993, 10799, 9250, 37133, 10909, 25304, 17137, 16924, 36750, 10816, 25041, 35561, 31987, 10801, 88155, 79744, 13687, 97646, 25498, 28678, 26238, 16533, 25147, 10793, 9492, 32536, 38193, 37028, 69738, 37128, 36630, 10953, 25067, 79265, 90356, 37350, 18388, 38879, 90536, 80853, 3323, 11967, 89430, 38496, 25958, 13091, 80625, 1834, 14424, 123854, 131411, 69668, 17092, 24460, 28619, 10820, 79947, 79504, 35299, 80423, 28549, 16504, 38196, 71977, 90351, 25306, 35381, 13224, 35552, 86166, 24952, 17498, 96154, 25309, 35548, 13948, 62519, 35209, 36955, 35705, 123894, 25511, 39148, 85927, 77593, 35295, 38845, 25182, 92136, 58308, 32373, 9212, 10778, 38858, 93529, 31543, 17955, 35308, 39152, 35270, 21128, 38214, 37097, 10621, 96215, 37058, 37329, 49770, 90321, 57826, 18285, 91621, 129295, 96001, 39678, 18365, 95892, 38437, 69505, 31799, 80833, 9405, 80909, 100040, 79488, 37192, 63739, 25476, 92003, 91718, 38679, 22122, 36877, 28618, 92025, 35701, 115680, 10455, 36838, 10789, 17400, 9384, 54828, 90336, 26201, 58883, 35326, 20736, 58580, 79955, 39002, 10117, 38953, 35183, 24250, 10543, 12907, 25282, 13105, 25213, 13560, 10942, 13677
 },
 new int[] { 9535, 17130, 25307, 17302, 17175, 25286, 79416, 16365, 37258, 79697, 16826, 11946, 35549, 25044, 9842, 38649, 36775, 3101, 47055, 38626, 65971, 10655, 36918, 20878, 79464, 13808, 23981, 16625, 17231, 17307, 9684, 50913, 10459, 1886, 87451, 38750, 69280, 35323, 32391, 65893, 3123, 37079, 26166, 25320, 37108, 16599, 11772, 91619, 35809, 16820, 38278, 37194, 26140, 79400, 16568, 11137, 92008, 80278, 17227, 29380, 55026, 37346, 36874, 14805, 10798, 36829, 10883, 133597, 3502, 3354, 37169, 72259, 16663, 20990, 123850, 58319, 37419, 96459, 21980, 17503, 17430, 16291, 20902, 38748, 96958, 17404, 80584, 11828, 17373, 96390, 82909, 16618, 62271, 18361, 91931, 3758, 131408, 3869, 10413, 79625, 101238, 15173, 65962, 14287, 20873, 25479, 31970, 95895, 79448, 35536, 3681, 77809, 37246, 9061, 35821, 51673, 3860, 55423, 16950, 72877, 133643, 86225, 77555, 13015, 24223, 10081, 16989, 67138, 39539, 119792, 11750, 86002, 26205, 95956, 10948, 3872, 25501, 1185, 72034, 38795, 80361, 20633, 83135, 3643, 38298, 79859, 91808, 11972, 38402, 13867, 17856, 79900, 16657, 79764, 16998, 10559, 16824, 21400, 23959, 77634, 3667, 133835, 10932, 3404, 16688, 1181, 17124, 16832, 15007, 10525
 }};

            var boundaries = new decimal[]{
                -7.5866635357567683545779447525M,
                -17.217427947597800497576543527M,
                -18.947083064615648402780217446M,
                -19.909148571440727100218398278M,
                -20.313326477449819516008280692M
            };

            var rsList = new List<Dictionary<int, decimal>>();
            for (int i = 0; i < 1; i++)
                rsList.Add(Methods.GeneralizedAssessment.FindNonContiniousFeature(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), activeFeatures[i]));


            using (var resultFile = new StreamWriter($"Result file {DateTime.Now: yyyyMMdd HHmmss}"))
                foreach (var testObject in testObjects)
                {
                    var classValue = new int?[1];
                    for (int i = 0; i < 1; i++)
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
                        logger.WriteLine($"Test rs {i}", $"{boundaries[i]}\t{rs}\t{classValue[i]}");
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
