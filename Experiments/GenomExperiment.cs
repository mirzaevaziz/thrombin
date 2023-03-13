using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace thrombin.Experiments;

public class GenomExperiment
{
    public static void RunX4()
    {
        var logger = new Helpers.Logger($"GenomExperiment RunX4 {DateTime.Now:yyyyMMdd HHmmss}");

        var trainSet = Models.ObjectSet.FromFileData(Path.Combine("ARF4-205_unique.dat"), 0);
        logger.WriteLine("SetInfo", trainSet.ToString(), true);

        var values = trainSet.Objects.Select(s => s[4]).Distinct();
        logger.WriteLine("Result", string.Join(", ", values));
        var f = new Dictionary<decimal, decimal>();
        foreach (var val in values)
        {
            decimal k1 = trainSet.Objects.Count(w => w[4] == val && w.ClassValue == trainSet.ClassValue);
            decimal k2 = trainSet.Objects.Count(w => w[4] == val && w.ClassValue != trainSet.ClassValue);

            f[val] = (k1 / trainSet.ClassObjectCount) / ((k1 / trainSet.ClassObjectCount) + (k2 / trainSet.NonClassObjectCount));

            logger.WriteLine("Result", $"Value={val} k1={k1} k2={k2} f({val})={f[val]}");
        }
    }
    public static void Run()
    {
        var logger = new Helpers.Logger($"GenomExperiment {DateTime.Now:yyyyMMdd HHmmss}");

        var trainSet = Models.ObjectSet.FromFileData(Path.Combine("ARF4-205_unique.dat"), 0);
        logger.WriteLine("SetInfo", trainSet.ToString(), true);

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

        var newFeaturesCount = 9;
        var orderedFeatures = trainSetFeatureWeights.Where(w => w.Value.Value > 0).OrderByDescending(o => o.Value.Value).Select(s => s.Key).ToList();

        var newFeatures = new Models.Feature[newFeaturesCount];

        var rs = new Dictionary<int, Dictionary<int, decimal>>();
        var boundary = new Dictionary<int, decimal>();
        for (int i = 0; i < newFeaturesCount; i++)
        {
            newFeatures[i] = new Models.Feature { Name = $"Ft of RS {i}", IsContinuous = true };
            var firstPair = new List<int>() { orderedFeatures[0] };
            var featuresSet = new List<int>();
            if (orderedFeatures.Count > 1)
                featuresSet = Methods.FindAllFeaturesByRs.Find(trainSet, trainSetFeatureWeights.ToDictionary(k => k.Key, v => v.Value), logger, firstPair, orderedFeatures.Skip(1).ToHashSet());
            else
                featuresSet.AddRange(firstPair);
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
            logger.WriteLine($"0{i}. Set of features.txt", $"Min={rs[i].Min(s => s.Value)}");
            logger.WriteLine($"0{i}. Set of features.txt", $"Max={rs[i].Max(s => s.Value)}");

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

        var rsSet = new Models.ObjectSet("Method3DByRs set", rsObjects, newFeatures, trainSet.ClassValue);
        rsSet.ToFileData($"{trainSet.Name} New Rs DataSet.txt");

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

            decimal k1 = rsSet.Objects.Where(w => w[i] > boundary[i] && w.ClassValue == trainSet.ClassValue).Count();
            decimal k1All = rsSet.Objects.Where(w => w.ClassValue == 1).Count();
            decimal k2 = rsSet.Objects.Where(w => w[i] <= boundary[i] && w.ClassValue != trainSet.ClassValue).Count();
            decimal k2All = rsSet.Objects.Where(w => w.ClassValue != 1).Count();
            logger.WriteLine("Criterion 1 results", $"\tK1: {k1} ({k1All})");
            logger.WriteLine("Criterion 1 results", $"\tK2: {k2} ({k2All})");
            logger.WriteLine("Criterion 1 results", $"\tPercent: {((k1 + k2) / (decimal)rsSet.Objects.Length) * 100:00.00}%");
        }


        var distFunc = Metrics.MetricFunctionGetter.GetMetric(rsSet, "Method3DByRS set");
        decimal? maxCompactness = null;
        int[] maxFeatures = null;
        var combination = Combinations<int>(Enumerable.Range(0, 9));
        foreach (var comb in combination)
        {
            if (!comb.Any()) continue;

            var features = comb;
            System.Console.WriteLine(string.Join(", ", comb));
            //Finding standart objects 
            var excludedObjects = new HashSet<int>();

            var objects = new Models.ObjectInfo[rsSet.Objects.Length];
            for (int i = 0; i < trainSet.Objects.Length; i++)
            {
                objects[i] = new Models.ObjectInfo
                {
                    Index = i,
                    Data = Enumerable.Range(0, features.Count()).Select(s => rs[comb.ElementAt(s)][i]).ToArray(),
                    ClassValue = trainSet.Objects[i].ClassValue,
                };
            }

            var forStandartSet = new Models.ObjectSet("Method3DByRs set", objects, comb.Select(s => new Models.Feature { IsContinuous = true, Name = $"Ft{s}" }).ToArray(), trainSet.ClassValue);
            // rsSet.ToFileData($"{trainSet.Name} New Rs DataSet.txt");

            forStandartSet = Methods.NormilizingMinMax.Normalize(forStandartSet);



            // for (int i = 0; i < forStandartSet.Objects.Length; i++)
            // {
            //     var objectNeighborhood = new Models.ObjectNeighborhood(i, null);
            //     for (int j = 0; j < forStandartSet.Objects.Length; j++)
            //     {
            //         if (i == j) continue;

            //         var distance = distFunc(forStandartSet.Objects[i], forStandartSet.Objects[j], forStandartSet.Features, Enumerable.Range(0, forStandartSet.Features.Length));
            //         var neighbor = new Models.ObjectNeighborhood.ObjectNeighbor();
            //     }
            // }

            var distances = Utils.DistanceUtils.FindAllDistance(forStandartSet, distFunc);
            var spheres = Models.Sphere.FindAll(forStandartSet, distances, excludedObjects, true);
            var noisyObjects = Methods.FindNoisyObjects.Find(forStandartSet, spheres, excludedObjects, logger);
            excludedObjects.UnionWith(noisyObjects);
            distances = Utils.DistanceUtils.FindAllDistance(forStandartSet, distFunc, excludedObjects);
            spheres = Models.Sphere.FindAll(forStandartSet, distances, excludedObjects, true);
            var groups = Methods.FindAcquaintanceGrouping.Find(forStandartSet, spheres);
            var standartObject = Methods.FindStandartObjects.Find(forStandartSet, groups, spheres, distances, logger);
            logger.WriteLine("Result", "===============");
            logger.WriteLine("Result", $"Active features: {string.Join(", ", features.OrderBy(o => o))}");
            if (standartObject.Count > 0)
            {
                var compactness = ((forStandartSet.Objects.Length - noisyObjects.Count) / (decimal)forStandartSet.Objects.Length) * ((forStandartSet.Objects.Length - noisyObjects.Count) / (decimal)standartObject.Count);

                logger.WriteLine("Result", $"Compactness: {compactness}");

                if (!maxCompactness.HasValue || maxCompactness < compactness)
                {
                    maxCompactness = compactness;
                    maxFeatures = features;
                }
            }
            logger.WriteLine("Result", $"Noisy objects ({noisyObjects.Count}): {string.Join(", ", noisyObjects.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Standart objects ({standartObject.Count}): {string.Join(", ", standartObject.OrderBy(o => o))}");
            logger.WriteLine("Result", $"Groups ({groups.Count}): {string.Join(Environment.NewLine, groups.Select(s => $"{{{string.Join(", ", s)}}}"))}");
        }

        logger.WriteLine("Result", $"Max ({maxCompactness}): {string.Join(", ", maxFeatures.OrderBy(o => o))}");
    }

    public static IEnumerable<T[]> Combinations<T>(IEnumerable<T> source)
    {
        if (null == source)
            throw new ArgumentNullException(nameof(source));

        T[] data = source.ToArray();

        return Enumerable
          .Range(0, (1 << (data.Length)) - 1)
          .Select(index => data
             .Where((v, i) => (index & (1 << i)) != 0)
             .ToArray());
    }

    public static void RunToUniqueSet()
    {
        var logger = new Helpers.Logger($"GenomExperiment to unique set {DateTime.Now:yyyyMMdd HHmmss}");

        var set = Models.ObjectSet.FromFileData(Path.Combine("Data", "ARF4-205.dat"), 0);
        logger.WriteLine("SetInfo", set.ToString(), true);

        var notUniqueIndexes = new HashSet<int>();
        for (int i = 0; i < set.Objects.Length - 1; i++)
        {
            if (notUniqueIndexes.Contains(i)) continue;
            for (int j = i + 1; j < set.Objects.Length; j++)
            {
                if (notUniqueIndexes.Contains(j)) continue;

                if (set.Objects[i].EqualsByValues(set.Objects[j]))
                {
                    notUniqueIndexes.Add(j);
                    if (set.Objects[i].ClassValue != set.Objects[j].ClassValue
                    && (set.Objects[i].ClassValue == 0 || set.Objects[j].ClassValue == 0))
                    {
                        logger.WriteLine("Not unique and class are not same", $"{i:0000} == {j:0000} and class {set.Objects[i].ClassValue} != {set.Objects[j].ClassValue}");
                        notUniqueIndexes.Add(i);
                    }
                }
            }
        }

        logger.WriteLine("Not Unique objects indexes", string.Join(Environment.NewLine, notUniqueIndexes), true);

        set.ToFileData("ARF4-205_unique.dat", notUniqueIndexes, Enumerable.Range(0, 27).ToList());
    }
}