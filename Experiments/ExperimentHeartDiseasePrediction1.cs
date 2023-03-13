namespace thrombin.Experiments;

class ExperimentHeartDiseasePrediction1
{
    public static void TransformToNonContinuous()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss} - TransformToNonContinuous");

        var set = thrombin.Data.HeartDataSetProvider.ReadDataSetUnique(logger);
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

    public static void TransformUniqueToNonContinuous()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss} - TransformToNonContinuous");

        var set = thrombin.Data.HeartDataSetProvider.ReadDataSetUnique(logger);
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

                foreach (var obj in set.Objects)
                {
                    var interval = c.First(w => w.ObjectValueStart <= obj[i] && obj[i] <= w.ObjectValueEnd);
                    obj[i] = (interval.FunctionValue > 0.5M) ? 1 : 0;
                }

                // for (int j = 0; j < c.Count(); j++)
                // {
                //     var boundary = c.ElementAt(j);
                //     var objs = set.Objects.Where(w => boundary.ObjectValueStart <= w[i] && w[i] <= boundary.ObjectValueEnd);
                //     foreach (var obj in objs)
                //     {
                //         obj[i] = j;
                //     }
                // }
                set.Features[i].IsContinuous = false;
            }
        }

        set.ToFileData("ExperimentHeartDiseasePrediction1AllNonContinuous.txt");
    }

    public static void Run()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "HeartDiseasePredictionAllNonContinuous.txt"), 0);

        logger.WriteLine("Set info", set.ToString(), true);

        var featureGradationContribute = new Dictionary<int, Dictionary<decimal, decimal>>();
        var trainSetFeatureWeights = new Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
        // for (int ft = 0; ft < set.Features.Length; ft++)
        // {
        //     var gradationList = set.Objects.Select(s => s[ft]).Distinct().ToArray();
        //     var gradationContribute = new Dictionary<decimal, decimal>();
        //     foreach (var gradient in gradationList)
        //     {
        //         decimal n1 = set.Objects.Count(w => w[ft] == gradient && w.ClassValue == set.ClassValue) / (decimal)set.Objects.Count(w => w.ClassValue == set.ClassValue);
        //         decimal n2 = set.Objects.Count(w => w[ft] == gradient && w.ClassValue != set.ClassValue) / (decimal)set.Objects.Count(w => w.ClassValue != set.ClassValue);
        //         gradationContribute[gradient] = n1 / (n1 + n2);
        //     }
        //     featureGradationContribute[ft] = gradationContribute;
        // }

        // foreach (var i in featureGradationContribute.Keys)
        // {
        //     for (int j = 0; j < set.Objects.Length; j++)
        //     {
        //         if (featureGradationContribute[i][set.Objects[j][i]] > 0.5M)
        //         {
        //             set.Objects[j][i] = 1;
        //         }
        //         else if (featureGradationContribute[i][set.Objects[j][i]] < 0.5M)
        //         {
        //             set.Objects[j][i] = 2;
        //         }
        //         else set.Objects[j][i] = 3;
        //     }

        //     foreach (var j in featureGradationContribute[i].Keys)
        //     {
        //         System.Console.WriteLine($"Feature[{i}]  grad[{j}] \t{featureGradationContribute[i][j]}");
        //     }
        // }

        for (int i = 0; i < set.Features.Length; i++)
        {
            var p = set.Objects.Select(s => new Criterions.IntervalCriterion.IntervalCriterionParameter
            {
                ClassValue = s.ClassValue.Value,
                Distance = s[i],
                ObjectIndex = s.Index
            });
            var c = Criterions.IntervalCriterion.Find(p, set.ClassValue);
            logger.WriteLine($"Result for feature interval criterion #{i:00}", string.Join("\n", c));

            for (int objIndex = 0; objIndex < set.Objects.Length; objIndex++)
            {
                var interval = c.First(w => w.ObjectValueStart <= set.Objects[objIndex][i] && w.ObjectValueEnd >= set.Objects[objIndex][i]);
                if (interval.FunctionValue > 0.5M)
                {
                    set.Objects[objIndex][i] = 0;
                }
                else
                {
                    set.Objects[objIndex][i] = 1;
                }
            }

            for (int j = 0; j < c.Count(); j++)
            {
                System.Console.WriteLine($"\t{c.ElementAt(j)}");
            }
        }

        set.ToFileData("ExperimentHeartDiseasePrediction1_NewNonContinuousSet");

        featureGradationContribute = new Dictionary<int, Dictionary<decimal, decimal>>();
        for (int ft = 0; ft < set.Features.Length; ft++)
        {
            var gradationList = set.Objects.Select(s => s[ft]).Distinct().ToArray();
            var gradationContribute = new Dictionary<decimal, decimal>();
            foreach (var gradient in gradationList)
            {
                decimal n1 = set.Objects.Count(w => w[ft] == gradient && w.ClassValue == set.ClassValue) / (decimal)set.Objects.Count(w => w.ClassValue == set.ClassValue);
                decimal n2 = set.Objects.Count(w => w[ft] == gradient && w.ClassValue != set.ClassValue) / (decimal)set.Objects.Count(w => w.ClassValue != set.ClassValue);
                gradationContribute[gradient] = n1 / (n1 + n2);
            }
            featureGradationContribute[ft] = gradationContribute;
        }

        var featureStability = new Dictionary<int, decimal>();
        foreach (var i in featureGradationContribute.Keys)
        {
            featureStability[i] = 0;
            for (int j = 0; j < set.Objects.Length; j++)
            {
                if (featureGradationContribute[i][set.Objects[j][i]] > 0.5M)
                {
                    featureStability[i] += featureGradationContribute[i][set.Objects[j][i]];
                }
                else if (featureGradationContribute[i][set.Objects[j][i]] < 0.5M)
                {
                    featureStability[i] += 1 - featureGradationContribute[i][set.Objects[j][i]];
                }
            }
            featureStability[i] = featureStability[i] / set.Objects.Length;
            trainSetFeatureWeights[i] = Criterions.NonContinuousFeatureCriterion.Find(set.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter
            {
                ClassValue = s.ClassValue.Value,
                FeatureValue = s[i],
                ObjectIndex = s.Index
            }), set.ClassValue);

            System.Console.WriteLine($"Feature[{i}] Stability\t{featureStability[i]}");
        }

        // // var orderedFeatures = trainSetFeatureWeights.Where(w => w.Value.Value > 0).OrderByDescending(o => o.Value.Value).Select(s => s.Key).ToList();

        var orderedFeatures = featureStability.Where(w => w.Value > 0).OrderByDescending(o => o.Value).Select(s => s.Key).ToList();


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

            // var objRS = new Dictionary<int, decimal>();
            // for (int i = 0; i < set.Objects.Length; i++)
            // {
            //     foreach (var ft in featuresSet)
            //     {
            //         if (objRS.ContainsKey(i))
            //             objRS[i] += featureGradationContribute[ft][set.Objects[i][ft]];
            //         else
            //             objRS[i] = featureGradationContribute[ft][set.Objects[i][ft]];
            //     }
            // }

            rs[ftIndex] = Methods.GeneralizedAssessment.FindNonContiniousFeature(set, trainSetFeatureWeights, featuresSet);

            var param = rs[ftIndex].Select(s => new Criterions.FirstCriterion.FirstCriterionParameter
            {
                ClassValue = set.Objects[s.Key].ClassValue.Value,
                Distance = s.Value,
                ObjectIndex = s.Key
            });

            var crit1Result = Criterions.FirstCriterion.Find(param, set.ClassValue);

            boundary[ftIndex] = (crit1Result.Distance + rs[ftIndex].Where(w => w.Value > crit1Result.Distance).Min(m => m.Value)) / 2M;

            logger.WriteLine($"0{ftIndex}. Set of features.txt", $"Feature count is {featuresSet.Count()}\nCriterion1 result is {crit1Result}\nBoundary = {boundary[ftIndex]}");
            logger.WriteLine($"0{ftIndex}. Set of features.txt", string.Join("\n", rs[ftIndex].Values.Select(s => $"{s:0.000000}")));


            orderedFeatures = orderedFeatures.Except(featuresSet).ToList();
        }

        for (int i = 0; i < rs[0].Count; i++)
        {
            for (int j = 0; j <= ftIndex; j++)
            {
                logger.Write("New set", $"{rs[j][i]}\t");
            }
            logger.WriteLine("New set", $"{set.Objects[i].ClassValue}");
        }
    }

    public static void Run3()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction1 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "Fazo_Lm17.dat"), 1);

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

                var found = p.Count(obj => c.Distance < obj.Distance && obj.ClassValue == set.ClassValue || c.Distance >= obj.Distance && obj.ClassValue != set.ClassValue);

                System.Console.WriteLine($"{c.Distance}");

                System.Console.WriteLine($"{p.Count(obj => c.Distance >= obj.Distance && obj.ClassValue == set.ClassValue)} -- {p.Count(obj => c.Distance >= obj.Distance && obj.ClassValue != set.ClassValue)}");

                System.Console.WriteLine($"{p.Count(obj => c.Distance < obj.Distance && obj.ClassValue == set.ClassValue)} -- {p.Count(obj => c.Distance < obj.Distance && obj.ClassValue != set.ClassValue)}");

                System.Console.WriteLine($"Found {found};\t Accuracy {found / (decimal)p.Count()}");
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

    public static void Run4()
    {
        var logger = new Helpers.Logger($"ExperimentHeartDiseasePrediction Run4 {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "Fazo_Lm17.dat"), 1);

        logger.WriteLine("Set info", set.ToString(), true);

        set = Methods.NormilizingMinMax.Normalize(set);

        var distFunc = Metrics.MetricFunctionGetter.GetMetric(set, "For distance");

        // System.Console.WriteLine($"Finding all distances at {DateTime.Now}...");
        // var dist = Utils.DistanceUtils.FindAllDistance(set, distFunc);

        System.Console.WriteLine($"Finding all spheres at {DateTime.Now}...");
        var spheres = Models.Sphere.FindAll(set, distFunc, null, true);
        System.Console.WriteLine($"Founded {spheres.Count()} spheres...");

        logger.WriteLine("Spheres with noisy objects", string.Join(Environment.NewLine, spheres.OrderBy(o => o.ObjectIndex)));

        logger.WriteLine("Boundary objects with noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Enemies).Distinct().OrderBy(o => o)));

        logger.WriteLine("Coverage objects with noisy", string.Join(Environment.NewLine, spheres.SelectMany(s => s.Coverage).Distinct().OrderBy(o => o)));

        var noisyObjects = Methods.FindNoisyObjects.Find(set, spheres, logger);
        System.Console.WriteLine($"Noisy objects ({noisyObjects.Count}){{{string.Join(", ", noisyObjects)}}}");

        System.Console.WriteLine($"Finding all spheres at {DateTime.Now}...");
        spheres = Models.Sphere.FindAll(set, distFunc, noisyObjects, true);
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

        var standartObject = Methods.FindStandartObjects.Find(set, groups, spheres, distFunc, logger);

        System.Console.WriteLine($"Standart object ({standartObject.Count}) {{{string.Join(", ", standartObject)}}}");
    }
}

