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

        public static IEnumerable<Sphere> FindAll(ObjectSet set, DistanceAndRadius dist, HashSet<int> excludedObjects, bool ShouldFindCoverage = true)
        {
            var result = new BlockingCollection<Sphere>();

            Parallel.For(0, set.Objects.Length, i =>
            {
                if (excludedObjects?.Contains(i) == true)
                    return;

                var sphere = new Sphere()
                {
                    Radius = dist.Radiuses[i],
                    ObjectIndex = i
                };

                for (int j = 0; j < set.Objects.Length; j++)
                {
                    if (excludedObjects?.Contains(j) == true)
                        continue;

                    if (set.Objects[i].ClassValue == set.Objects[j].ClassValue && sphere.Radius > dist.Distances[i, j])
                    {
                        sphere.Relatives.Add(j);
                    }
                    if (set.Objects[i].ClassValue != set.Objects[j].ClassValue && sphere.Radius == dist.Distances[i, j])
                    {
                        sphere.Enemies.Add(j);
                    }
                }

                if (ShouldFindCoverage)
                {
                    foreach (var enemyIndex in sphere.Enemies)
                    {
                        decimal radius = decimal.MaxValue;
                        foreach (var j in sphere.Relatives)
                        {
                            if (radius >= dist.Distances[enemyIndex, j])
                            {
                                if (radius != dist.Distances[enemyIndex, j])
                                {
                                    sphere.Coverage.Clear();
                                }
                                radius = dist.Distances[enemyIndex, j];
                                sphere.Coverage.Add(j);
                            }
                        }
                    }
                }

                result.Add(sphere);
            });

            result.CompleteAdding();

            return result;
        }
    }
}