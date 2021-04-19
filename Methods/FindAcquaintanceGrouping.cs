using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindAcquaintanceGrouping
    {
        public static List<HashSet<int>> Find(ObjectSet set, IEnumerable<Sphere> spheres, HashSet<int> excludedObjects)
        {
            var result = new List<HashSet<int>>();

            var coverage = spheres.SelectMany(s => s.Coverage).ToHashSet();
            var allCoverage = spheres.SelectMany(s => s.Coverage).ToHashSet();
            var notSeenObjects = spheres.Select(s => s.ObjectIndex.Value).ToHashSet();

            while (coverage.Count > 0)
            {
                var obj = coverage.ElementAt(0);
                coverage.Remove(obj);
                var group = spheres.Where(w => w.Relatives.Contains(obj) && notSeenObjects.Contains(w.ObjectIndex.Value)).Select(s => s.ObjectIndex.Value).ToHashSet();
                if (group.Count == 0)
                    continue;
                do
                {
                    var cov = spheres.Where(w => group.Contains(w.ObjectIndex.Value) && w.Relatives.Any(a => allCoverage.Contains(a))).SelectMany(s => allCoverage.Where(w => s.Relatives.Contains(w))).ToHashSet();
                    notSeenObjects.RemoveWhere(w => group.Contains(w));
                    var newObjects = spheres.Where(w => notSeenObjects.Contains(w.ObjectIndex.Value) && w.Relatives.Any(a => cov.Contains(a))).Select(s => s.ObjectIndex.Value).ToHashSet();
                    if (newObjects.Count() == 0)
                        break;
                    group.UnionWith(newObjects);
                    coverage.RemoveWhere(w => cov.Contains(w));
                } while (true);
                result.Add(group);
            }

            return result;
        }
    }
}