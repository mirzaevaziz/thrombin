using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace thrombin.Models
{
    public class ObjectSet
    {
        public ObjectInfo[] Objects { get; }

        public Feature[] Features { get; }

        public int ClassValue { get; }

        public string Name { get; }

        public ObjectSet(string name, ObjectInfo[] objects, Feature[] features, int classValue = 1)
        {
            Name = name;
            Objects = objects;
            Features = features;
            ClassValue = classValue;

            if (objects.Length == 0)
                throw new System.ArgumentException("Objects weren't given.");

            if (features.Length == 0)
                throw new System.ArgumentException("Features weren't given.");

            if (objects.Any(a => !a.ClassValue.HasValue))
                throw new System.ArgumentException("Objects class weren't given.");

            foreach (var item in objects)
            {
                if (features.Length != item.Data.Length)
                    throw new System.ArgumentException($"Length of objects #{item.Index} columns doesn't match to features length.");
            }
        }

        public override string ToString()
        {
            return $@"ObjectSet = ""{Name}""
               , Objects count = {Objects.Length} 
               , Features count = {Features.Length} 
               , Class value = {ClassValue}
               , Class objects = {ClassObjectCount}
               , Non Class objects = {NonClassObjectCount}
               , ClassValues = {string.Join(", ", GetClassValues())}";
        }

        public int ClassObjectCount { get { return Objects.Count(w => w.ClassValue == ClassValue); } }
        public int NonClassObjectCount { get { return Objects.Count(w => w.ClassValue != ClassValue); } }
        public IEnumerable<int> GetClassValues()
        {
            return Objects.Select(s => s.ClassValue.GetValueOrDefault(-1)).Distinct();
        }

        internal void ToFileData(string path)
        {
            using (var file = new StreamWriter(path + ".features"))
            {
                file.WriteLine("IsContinuous|Name");
                file.WriteLine("5|Class feature");
                foreach (var ft in Features)
                {
                    file.WriteLine($"{ft.IsContinuous}|{ft.Name}");
                }
            }

            using (var file = new StreamWriter(path))
            {
                for (int i = 0; i < Objects.Length; i++)
                {
                    file.WriteLine($"{Objects[i].ClassValue}\t{string.Join('\t', Objects[i].Data)}");
                }
            }
        }

        internal void ToFileData(string path, HashSet<int> deletedObjects, List<int> activeFeatures)
        {
            using (var file = new StreamWriter(path + ".features"))
            {
                file.WriteLine("IsContinuous|Name");
                file.WriteLine("5|Class feature");
                for (int i = 0; i < Features.Length; i++)
                {
                    if (activeFeatures.Contains(i))
                    {
                        var ft = Features[i];
                        file.WriteLine($"{ft.IsContinuous}|{ft.Name}");
                    }
                }
            }


            using (var indexes = new StreamWriter(path + ".indexes"))
            using (var file = new StreamWriter(path))
            {
                for (int i = 0; i < Objects.Length; i++)
                {
                    if (deletedObjects.Contains(i)) continue;

                    indexes.WriteLine(i);
                    file.Write(Objects[i].ClassValue);
                    for (int ft = 0; ft < this.Features.Length; ft++)
                    {
                        if (activeFeatures.Contains(ft))
                        {
                            file.Write($"\t{Objects[i][ft]}");
                        }
                    }
                    file.WriteLine();
                }
            }
        }
    }
}