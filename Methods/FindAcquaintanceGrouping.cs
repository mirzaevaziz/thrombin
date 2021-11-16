using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindAcquaintanceGrouping
    {
        public static List<HashSet<int>> Find(ObjectSet set, IEnumerable<Sphere> spheres)
        {
            var result = new List<HashSet<int>>();

            var allCoverage = spheres.SelectMany(s => s.Coverage).ToHashSet();
            var notSeenObjects = spheres.Select(s => s.ObjectIndex.Value).ToHashSet();

            while (notSeenObjects.Count > 0)
            {
                var obj = notSeenObjects.ElementAt(0);
                // notSeenObjects.Remove(obj);
                var group = new HashSet<int>();
                group.Add(obj);

                while (true)
                {
                    var cov = spheres.Where(w => group.Contains(w.ObjectIndex.Value) || w.Relatives.Overlaps(group)).SelectMany(s => s.Relatives).ToHashSet();
                    cov.UnionWith(spheres.Where(w => group.Contains(w.ObjectIndex.Value) || w.Relatives.Overlaps(group)).Select(s => s.ObjectIndex.Value).ToHashSet());
                    cov.IntersectWith(allCoverage);
                    group.UnionWith(cov.Intersect(notSeenObjects));
                    notSeenObjects.RemoveWhere(w => group.Contains(w));

                    var newObjects = spheres.Where(w => notSeenObjects.Contains(w.ObjectIndex.Value) && (w.Relatives.Overlaps(cov) || cov.Contains(w.ObjectIndex.Value))).Select(s => s.ObjectIndex.Value);
                    if (newObjects.Count() == 0)
                        break;

                    group.UnionWith(newObjects);
                    notSeenObjects.RemoveWhere(w => group.Contains(w));
                }

                result.Add(group);
            }

            return result;
        }
    }
}