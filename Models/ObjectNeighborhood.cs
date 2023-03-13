using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace thrombin.Models
{
    public class ObjectNeighborhood
    {
        public class ObjectNeighbor
        {
            public ObjectNeighbor(int objectIndex, decimal distance, bool hasSameClass)
            {
                ObjectIndex = objectIndex;
                Distance = distance;
                HasSameClass = hasSameClass;
            }

            public int ObjectIndex { get; set; }
            public decimal Distance { get; set; }
            public bool HasSameClass { get; set; }
        }

        public int ObjectIndex { get; set; }
        public ObjectNeighbor[] NeighborList { get; set; }

        public ObjectNeighborhood(int objectIndex, ObjectNeighbor[] neighborList)
        {
            NeighborList = neighborList;
            ObjectIndex = objectIndex;
        }

        // public override string ToString()
        // {
        //     return $@"ObjectNeighborhood {ObjectIndex}: radius = {Radius}
        //        relatives = ({Relatives.Count}) {{{string.Join(", ", Relatives.OrderBy(o => o))}}}
        //        enemies= ({Enemies.Count}) {{{string.Join(", ", Enemies.OrderBy(o => o))}}}
        //        coverage= ({Coverage.Count}) {{{string.Join(", ", Coverage.OrderBy(o => o))}}}";
        // }

        public static IEnumerable<ObjectNeighborhood> FindAll(ObjectSet set, Utils.DistanceUtils.DistanceList dist, HashSet<int> excludedObjects)
        {
            var result = new BlockingCollection<ObjectNeighborhood>();

            var objects = Enumerable.Range(0, set.Objects.Length);
            if (excludedObjects?.Count > 0)
                objects = objects.Where(w => !excludedObjects.Contains(w));

            Parallel.ForEach(objects, i =>
            {
                var objectNeighborhood = new ObjectNeighborhood(i, objects.Where(w => w != i).Select(s => new ObjectNeighbor(
                    s,
                    dist[i, s],
                    set.Objects[s].ClassValue == set.Objects[i].ClassValue
                )).OrderBy(o => o.Distance).ToArray());

                result.Add(objectNeighborhood);
            });

            result.CompleteAdding();

            return result;
        }
    }
}