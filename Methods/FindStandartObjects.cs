using System;
using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindStandartObjects
    {
        public static HashSet<int> Find(ObjectSet set,
                                           IEnumerable<HashSet<int>> groups,
                                           IEnumerable<Sphere> spheres,
                                           HashSet<int> excludedObjects,
                                           DistanceAndRadius distances,
                                           Helpers.Logger log)
        {
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Started at {DateTime.Now}", true);
            var objects = groups.SelectMany(s => s).ToHashSet<int>().Except(excludedObjects);
            var standartObjects = spheres.Where(w => objects.Contains(w.ObjectIndex.Value))
                                        .Select(s => new
                                        {
                                            ObjectIndex = s.ObjectIndex.Value,
                                            Radius = s.Radius,
                                        }).ToList();
            foreach (var group in groups.OrderByDescending(o => o.Count))
            {
                var candidates = standartObjects.Where(w => group.Contains(w.ObjectIndex))
                                                .OrderBy(o => o.Radius).ToArray();

                foreach (var candidate in candidates)
                {
                    standartObjects.Remove(candidate);
                    foreach (var obj in objects)
                    {
                        decimal? minDistance = null;
                        bool isWrongRecognition = false;
                        foreach (var st in standartObjects)
                        {
                            var dist = distances.Distances[obj, st.ObjectIndex] / st.Radius;
                            if (!minDistance.HasValue || minDistance > dist)
                            {
                                minDistance = dist;
                                isWrongRecognition = set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                            else if (dist == minDistance)
                            {
                                isWrongRecognition = isWrongRecognition || set.Objects[st.ObjectIndex].ClassValue != set.Objects[obj].ClassValue;
                            }
                        }

                        if (isWrongRecognition)
                        {
                            standartObjects.Add(candidate);
                            log.WriteLine("FindStandartObjects", $"{candidate:000}");
                            break;
                        }
                    }
                }
            }
            log.WriteLine("FindStandartObjects", $"===== FindStandartObjects Ended at {DateTime.Now}", true);
            return standartObjects.Select(s => s.ObjectIndex).ToHashSet<int>();
        }
    }
}