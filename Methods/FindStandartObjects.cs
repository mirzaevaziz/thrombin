using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using thrombin.Metrics;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindStandartObjects
    {
        public static HashSet<int> Find(ObjectSet set,
                                           IEnumerable<HashSet<int>> groups,
                                           IEnumerable<Sphere> spheres,
                                           Utils.DistanceUtils.DistanceList distances,
                                           Helpers.Logger log)
        {
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Started at {DateTime.Now}", true);
            var objects = groups.SelectMany(s => s).ToHashSet<int>();
            var standartObjects = spheres.Where(w => objects.Contains(w.ObjectIndex.Value))
                                        .Select(s => new
                                        {
                                            ObjectIndex = s.ObjectIndex.Value,
                                            Radius = s.Radius,
                                        }).ToList();
            var counter = 0;
            foreach (var group in groups.OrderByDescending(o => o.Count).ToArray())
            {
                var candidates = standartObjects.Where(w => group.Contains(w.ObjectIndex))
                                                .OrderBy(o => o.Radius);

                foreach (var candidate in candidates)
                {
                    System.Console.WriteLine($"{++counter:00000}. Seeing {candidate} with {standartObjects.Count} standarts {DateTime.Now}");
                    standartObjects.Remove(candidate);
                    bool isWrongRecognition = false;
                    object locker = new object();

                    // foreach (var obj in objects)
                    Parallel.ForEach(objects, obj =>
                    {
                        if (isWrongRecognition)
                            return;

                        bool isWrong = false;
                        decimal minDistance = decimal.MaxValue;
                        foreach (var st in standartObjects)
                        {
                            var dist = distances[obj, st.ObjectIndex] / st.Radius.Value;
                            if (minDistance > dist)
                            {
                                minDistance = dist;
                                isWrong = set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                            else if (dist == minDistance && !isWrong)
                            {
                                isWrong = set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                        }
                        if (isWrong)
                            lock (locker)
                            {
                                isWrongRecognition = true;
                            }
                    }
                    );
                    if (isWrongRecognition)
                    {
                        standartObjects.Add(candidate);
                        log.WriteLine("FindStandartObjects", $"{candidate:000}");
                    }
                }
            }
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Ended at {DateTime.Now}", true);
            return standartObjects.Select(s => s.ObjectIndex).ToHashSet<int>();
        }
        public static HashSet<int> Find(ObjectSet set,
                                                   IEnumerable<HashSet<int>> groups,
                                                   IEnumerable<Sphere> spheres,
                                                   MetricCalculateFunctionDelegate distFunc,
                                                   Helpers.Logger log)
        {
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Started at {DateTime.Now}", true);
            var objects = groups.SelectMany(s => s).ToHashSet<int>();
            var standartObjects = spheres.Where(w => objects.Contains(w.ObjectIndex.Value))
                                        .Select(s => new
                                        {
                                            ObjectIndex = s.ObjectIndex.Value,
                                            Radius = s.Radius,
                                        }).ToList();
            var counter = 0;
            foreach (var group in groups.OrderByDescending(o => o.Count).ToArray())
            {
                var candidates = standartObjects.Where(w => group.Contains(w.ObjectIndex))
                                                .OrderBy(o => o.Radius);

                foreach (var candidate in candidates)
                {
                    System.Console.WriteLine($"{++counter:00000}. Seeing {candidate} with {standartObjects.Count} standarts {DateTime.Now}");
                    standartObjects.Remove(candidate);
                    bool isWrongRecognition = false;
                    object locker = new object();

                    // foreach (var obj in objects)
                    Parallel.ForEach(objects, obj =>
                    {
                        if (isWrongRecognition)
                            return;

                        bool isWrong = false;
                        decimal minDistance = decimal.MaxValue;
                        foreach (var st in standartObjects)
                        {
                            var dist = distFunc(set.Objects[obj], set.Objects[st.ObjectIndex], set.Features, Enumerable.Range(0, set.Features.Length)) / st.Radius.Value;
                            if (minDistance > dist)
                            {
                                minDistance = dist;
                                isWrong = set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                            else if (dist == minDistance && !isWrong)
                            {
                                isWrong = set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                        }
                        if (isWrong)
                            lock (locker)
                            {
                                isWrongRecognition = true;
                            }
                    }
                    );
                    if (isWrongRecognition)
                    {
                        standartObjects.Add(candidate);
                        log.WriteLine("FindStandartObjects", $"{candidate:000}");
                    }
                }
            }
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Ended at {DateTime.Now}", true);
            return standartObjects.Select(s => s.ObjectIndex).ToHashSet<int>();
        }
    }
}