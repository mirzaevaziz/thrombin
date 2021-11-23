using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using thrombin.Models;

namespace thrombin.Methods
{
    public class FindNoisyObjects
    {
        public static HashSet<int> Find(
            ObjectSet set,
            IEnumerable<Sphere> spheres, Helpers.Logger log)
        {
            log.WriteLine("FindNoisyObjects", $"===== FindNoisyObjects Started at {DateTime.Now}", true);

            var result = new BlockingCollection<int>();

            var candidates = spheres.SelectMany(s => s.Enemies).Distinct();

            Parallel.ForEach(candidates, candidate =>
            {
                var relativeCount = spheres.First(w => w.ObjectIndex == candidate).Relatives.Count();

                var enemyCount = spheres.Count(w => w.Enemies.Contains(candidate));

                if (relativeCount < enemyCount)
                {
                    result.Add(candidate);
                    // log.WriteLine("FindNoisyObjects log", $"Object[{candidate}], Relative count = {relativeCount}, Enemy count = {enemyCount}");
                }

            });
            result.CompleteAdding();

            log.WriteLine("FindNoisyObjects", string.Join(Environment.NewLine, result.OrderBy(o => o)));

            log.WriteLine("FindNoisyObjects", $"===== FindNoisyObjects Ended at {DateTime.Now}", true);
            return result.ToHashSet();
        }

        public static HashSet<int> Find(
            ObjectSet set,
            IEnumerable<Sphere> spheres,
            HashSet<int> excludedObjects, Helpers.Logger log)
        {
            log.WriteLine("FindNoisyObjects", $"===== FindNoisyObjects Started at {DateTime.Now}", true);

            var result = new BlockingCollection<int>();

            decimal classCount = set.Objects.Count(w => w.ClassValue == set.ClassValue && !excludedObjects.Contains(w.Index));

            decimal nonclassCount = set.Objects.Count(w => w.ClassValue != set.ClassValue && !excludedObjects.Contains(w.Index));

            var candidates = spheres.SelectMany(s => s.Enemies).Distinct();

            Parallel.ForEach(candidates, candidate =>
            {
                var relativeCount = spheres.First(w => w.ObjectIndex == candidate).Relatives.Where(w => !excludedObjects.Contains(w)).Count();

                var enemyCount = spheres.Count(w => !excludedObjects.Contains(w.ObjectIndex.Value) && w.Enemies.Contains(candidate));

                if (set.Objects[candidate].ClassValue == set.ClassValue)
                {
                    if (relativeCount / classCount < enemyCount / nonclassCount)
                    {
                        result.Add(candidate);
                        log.WriteLine("FindNoisyObjects log", $"Object[{candidate}], Relative count = {relativeCount}, Enemy count = {enemyCount}, Class count = {classCount}, Non class count = {nonclassCount},\n\t\t{relativeCount / classCount} < {enemyCount / nonclassCount}");
                    }
                }
                else
                {
                    if (relativeCount / nonclassCount < enemyCount / classCount)
                    {
                        result.Add(candidate);
                        log.WriteLine("FindNoisyObjects log", $"Object[{candidate}], Relative count = {relativeCount}, Enemy count = {enemyCount}, Class count = {nonclassCount}, Non class count = {classCount},\n\t\t{relativeCount / nonclassCount} < {enemyCount / classCount}");
                    }
                }

            });
            result.CompleteAdding();

            log.WriteLine("FindNoisyObjects", string.Join(Environment.NewLine, result));

            log.WriteLine("FindNoisyObjects", $"===== FindNoisyObjects Ended at {DateTime.Now}", true);
            return result.ToHashSet();
        }

    }
}