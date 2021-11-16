using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace thrombin.Models
{
    public class Sphere
    {
        public int? ObjectIndex { get; set; }
        public decimal? Radius { get; set; }
        public HashSet<int> Relatives { get; set; }
        public HashSet<int> Enemies { get; set; }
        public HashSet<int> Coverage { get; private set; }

        public Sphere()
        {
            Relatives = new HashSet<int>();
            Enemies = new HashSet<int>();
            Coverage = new HashSet<int>();
        }

        public override string ToString()
        {
            return $@"Sphere {ObjectIndex}: radius = {Radius}
               relatives = ({Relatives.Count}) {{{string.Join(", ", Relatives.OrderBy(o => o))}}}
               enemies= ({Enemies.Count}) {{{string.Join(", ", Enemies.OrderBy(o => o))}}}
               coverage= ({Coverage.Count}) {{{string.Join(", ", Coverage.OrderBy(o => o))}}}";
        }

        public static IEnumerable<Sphere> FindAll(ObjectSet set, decimal[,] dist, HashSet<int> excludedObjects, bool ShouldFindCoverage = true)
        {
            var result = new BlockingCollection<Sphere>();

            var objects = Enumerable.Range(0, set.Objects.Length);
            if (excludedObjects?.Count > 0)
                objects = objects.Where(w => !excludedObjects.Contains(w));

            Parallel.ForEach(objects, i =>
            {
                var sphere = new Sphere()
                {
                    Radius = decimal.MaxValue,
                    ObjectIndex = i
                };

                foreach (int j in objects)
                {
                    if (set.Objects[i].ClassValue != set.Objects[j].ClassValue)
                    {
                        if (sphere.Radius > dist[i, j])
                        {
                            sphere.Radius = dist[i, j];
                            sphere.Enemies.Clear();
                            sphere.Enemies.Add(j);
                        }
                        else if (sphere.Radius == dist[i, j])
                        {
                            sphere.Enemies.Add(j);
                        }
                    }
                }

                foreach (int j in objects)
                {
                    if (i != j && set.Objects[i].ClassValue == set.Objects[j].ClassValue && sphere.Radius > dist[i, j])
                    {
                        sphere.Relatives.Add(j);
                    }
                }

                if (ShouldFindCoverage)
                {
                    foreach (var enemyIndex in sphere.Enemies)
                    {
                        var cov = new HashSet<int>();
                        decimal radius = decimal.MaxValue;
                        var indexes = sphere.Relatives.ToArray().Append(sphere.ObjectIndex.Value);
                        foreach (var j in indexes)
                        {
                            if (radius >= dist[enemyIndex, j])
                            {
                                if (radius != dist[enemyIndex, j])
                                {
                                    cov.Clear();
                                    radius = dist[enemyIndex, j];
                                }
                                cov.Add(j);
                            }
                        }
                        sphere.Coverage.UnionWith(cov);
                    }
                }

                result.Add(sphere);
            });

            result.CompleteAdding();

            return result;
        }
    }
}