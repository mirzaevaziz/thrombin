using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace thrombin
{
    class Program
    {
        static void Main(string[] args)
        {
            var uniqueSet = new Data.Train.ThrombinUniqueSet().GetSet();
            System.Console.WriteLine(uniqueSet);

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
            var standartObjectIndexes = new int[] { 508, 1034 };
            var informativeFeatures = new int[] { 634, 635, 642, 901, 905, 906, 907, 908, 909 };
            int objectIndex = -1;
            var pageSize = 100;
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
                        ClassValue = (line[0] == '1' ? 1 : 2),
                        Data = data
                    });
                }
            }
            System.Console.WriteLine($"Standart objects count is {standartObjects.Count}");

            var uniqueFeatureIndexList = uniqueFeatureWeights.Keys.OrderBy(o => o).ToList();
            var testObjects = new List<Models.ObjectInfo>();
            using (var testFile = new StreamReader(Path.Combine("Data", "Test", "Thrombin.testset")))
            {
                while (!testFile.EndOfStream)
                {
                    var line = testFile.ReadLine().Substring(2);
                    var data = new decimal[uniqueFeatureIndexList.Count];
                    int i = 0;
                    foreach (var ft in uniqueFeatureIndexList)
                    {
                        data[i++] = (line[(ft) * 2] == '0') ? 0 : 1;
                    }
                    testObjects.Add(new Models.ObjectInfo()
                    {
                        Data = data
                    });
                }
            }
            System.Console.WriteLine($"Test objects count is {testObjects.Count}");

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
                        resultFile.Write($"{rs:0.00000}\t");
                    }
                    resultFile.WriteLine();
                }

                foreach (var item in standartObjects)
                {
                    resultFile.WriteLine(item);
                }
            }

            using (var resultFile = new StreamWriter($"Result file {DateTime.Now: ddMMyyyy HH mm ss}"))
                foreach (var testObject in testObjects)
                {
                    var testRs = new Models.ObjectInfo();
                    testRs.Data = new decimal[informativeFeatures.Length];
                    int ind = 0;
                    foreach (var ft in informativeFeatures)
                    {
                        testRs[ind++] = Methods.GeneralizedAssessment.FindNonContiniousFeature(testObject, informativeFeaturesWeights[ft]);
                    }
                    decimal? minDist = null;
                    int? classValue = null;
                    foreach (var standartObject in standartObjects)
                    {
                        var dist = new Metrics.Euclidean().Calculate(testRs, standartObject, featureRs, activeFeatures);
                        if (minDist == null || minDist >= dist)
                        {
                            if (minDist == null || minDist > dist)
                            {
                                classValue = standartObject.ClassValue;
                            }
                            else if (classValue.HasValue && classValue != standartObject.ClassValue)
                            {
                                classValue = null;
                            }
                            minDist = dist;
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
