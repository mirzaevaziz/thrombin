using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.Train
{
    public class FizmatNNSet : IObjectSetProvider
    {
        public ObjectSet GetSet()
        {
            var features = Enumerable.Range(0, 487).Select(s => new Feature() { IsContinuous = false, Name = $"Ft {s:000000}" });

            var objects = new List<ObjectInfo>();
            int ind = 0;
            // using (var writer = new StreamWriter(Path.Combine("Data", "Dorothea", "thrombin_unique_set_True.data.new")))
            using (var reader = new StreamReader(Path.Combine("Data", "Train", "fizmatnn.txt")))
            {
                while (!reader.EndOfStream)
                {
                    var line = Array.ConvertAll(reader.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries), x => decimal.Parse(x));
                    var dt = new ObjectInfo()
                    {
                        ClassValue = (int)line[487],
                        Data = line.Take(487).ToArray(),
                        Index = ind++
                    };

                    objects.Add(dt);
                }
            }
            var newset = new ObjectSet(this.GetType().Name, objects.ToArray(), features.ToArray());
            return newset;
        }
    }
}