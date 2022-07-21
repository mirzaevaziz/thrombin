using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace thrombin.Experiments;

class ExperimentHeartDiseasePrediction1
{
    public static void TransformToNonContinuous()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss} - TransformToNonContinuous");

        var set = thrombin.Data.HeartDataSetProvider.ReadDataSet(logger);
        set.ToFileData("HeartDiseasePredictionRaw.txt");

        for (int i = 0; i < set.Features.Length; i++)
        {
            if (set.Features[i].IsContinuous)
            {
                logger.WriteLine("Continuous features", $"Feature[{i}] is continuous.", true);
                var p = set.Objects.Select(s => new Criterions.IntervalCriterion.IntervalCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    Distance = s[i],
                    ObjectIndex = s.Index
                });
                var c = Criterions.IntervalCriterion.Find(p, set.ClassValue);
                logger.WriteLine($"Result for feature interval criterion #{i:00}", string.Join("\n", c));

                for (int j = 0; j < c.Count(); j++)
                {
                    var boundary = c.ElementAt(j);
                    var objs = set.Objects.Where(w => boundary.ObjectValueStart <= w[i] && w[i] <= boundary.ObjectValueEnd);
                    foreach (var obj in objs)
                    {
                        obj[i] = j;
                    }
                }
                set.Features[i].IsContinuous = false;
            }
        }

        set.ToFileData("ExperimentHeartDiseasePrediction1AllNonContinuous.txt");
    }

    public static void Run()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "ExperimentHeartDiseasePrediction1AllNonContinuous.txt"));

        logger.WriteLine("Set info", set.ToString(), true);

        // Finding all features weights
        var trainSetFeatureWeights = new ConcurrentDictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
        Parallel.For(0, set.Features.Length, i =>
        {
            trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(set.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
            {
                ClassValue = s.ClassValue.Value,
                FeatureValue = s[i],
                ObjectIndex = s.Index
            }), set.ClassValue);
        });

        foreach (var item in trainSetFeatureWeights)
        {
            logger.WriteLine("Feature weights", $"Feature[{item.Key}] = {item.Value.Value}", true);
        }

        var orderedFeatures = trainSetFeatureWeights.Where(w => w.Value.Value > 0).OrderByDescending(o => o.Value.Value).Select(s => s.Key).ToList();

        var newFeatures = new List<Models.Feature>();
        var rs = new Dictionary<int, Dictionary<int, decimal>>();
        var boundary = new Dictionary<int, decimal>();
        var ftIndex = -1;
        while (orderedFeatures.Count > 0)
        {
            ftIndex++;
            newFeatures.Add(new Models.Feature { Name = $"Ft of RS {ftIndex}", IsContinuous = true });
            var featuresSet = orderedFeatures.Take(4);
            // var firstPair = new List<int>() { orderedFeatures[0] };
            // var featuresSet = Methods.FindAllFeaturesByRs.Find(set, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
            logger.WriteLine($"0{ftIndex}. Set of features.txt", string.Join(", ", featuresSet));

            rs[ftIndex] = Methods.GeneralizedAssessment.FindNonContiniousFeature(set, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), featuresSet);

            var crit1Result = Criterions.FirstCriterion.Find(rs[ftIndex].Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
            {
                ClassValue = set.Objects[s.Key].ClassValue.Value,
                Distance = s.Value,
                ObjectIndex = s.Key
            }), set.ClassValue);

            boundary[ftIndex] = (crit1Result.Distance + rs[ftIndex].Where(w => w.Value > crit1Result.Distance).Min(m => m.Value)) / 2M;

            logger.WriteLine($"0{ftIndex}. Set of features.txt", $"Feature count is {featuresSet.Count()}\nCriterion1 result is {crit1Result}\nBoundary = {boundary[ftIndex]}");
            logger.WriteLine($"0{ftIndex}. Set of features.txt", string.Join("\n", rs[ftIndex].Values.Select(s => $"{s:0.000000}")));

            orderedFeatures = orderedFeatures.Except(featuresSet).ToList();
        }
    }

    public static void Run3()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "Fazo_Lm17.dat"));

        logger.WriteLine("Set info", set.ToString(), true);

        Parallel.For(0, set.Features.Count(), i =>
        {
            if (set.Features[i].IsContinuous)
            {
                var p = set.Objects.Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    Distance = s[i],
                    ObjectIndex = s.Index
                }).ToArray();
                var c = Criterions.FirstCriterion.Find(p, set.ClassValue);
                logger.WriteLine($"Result for feature #{i:00}", c.ToString());
            }
            else
            {
                var p = set.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                {
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i],
                    ObjectIndex = s.Index
                }).ToArray();

                var c = Criterions.NonContinuousFeatureCriterion.Find(p, set.ClassValue);
                logger.WriteLine($"Result for feature #{i:00}", c.ToString());
            }
        });
    }

    public static void Run2()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 Run2 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "ExperimentHeartDiseasePrediction1AllNonContinuous.txt"));

        logger.WriteLine("Set info", set.ToString(), true);


        var max = new Tuple<string, decimal>("none", 0);
        // Max is ({8, 12}, 0.3941596889017089264361212492)     
        // Max is ({6, 10}, 0.3563956409448840647689795837)    
        // Max is ({3, 11}, 0.3050493665327349613101701012)   
        for (int i = 0; i < set.Features.Length - 1; i++)
        {
            if (i == 8 || i == 12 || i == 6 || i == 10) continue;

            for (int j = i + 1; j < set.Features.Length; j++)
            {
                if (j == 8 || j == 12 || j == 6 || j == 10) continue;

                var dict = new Dictionary<string, int>();

                var param = new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter[set.Objects.Length];
                int counter = 0;
                foreach (var obj in set.Objects)
                {
                    var key = $"{obj[i]}_{obj[j]}";
                    if (!dict.Keys.Contains(key))
                    {
                        dict[key] = dict.Keys.Count + 1;
                    }

                    param[counter++] = new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
                    {
                        ClassValue = obj.ClassValue.Value,
                        FeatureValue = dict[key],
                        ObjectIndex = obj.Index
                    };
                }

                var result = Criterions.NonContinuousFeatureCriterion.Find(param, set.ClassValue);

                var crit1Result = Criterions.FirstCriterion.Find(param.Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
                {
                    ClassValue = s.ClassValue,
                    Distance = result.FeatureContribute[s.FeatureValue],
                    ObjectIndex = s.ObjectIndex
                }), set.ClassValue);

                if (max.Item2 < crit1Result.Value)
                {
                    max = new Tuple<string, decimal>($"{{{i}, {j}}}", crit1Result.Value);
                }

                logger.WriteLine("Feature Pair Combination", $"{{{i}, {j}}} = {result.Value}\t{crit1Result}", true);
            }
        }

        logger.WriteLine("Feature Pair Combination", $"Max is {max}", true);
    }
}

