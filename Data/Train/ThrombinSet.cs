using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.Train
{
    public class ThrombinSet : IObjectSetProvider
    {
        public ObjectSet GetSet()
        {
            var features = Enumerable.Range(0, 139351).Select(s => new Feature() { IsContinuous = false, Name = $"Ft {s:000000}" });

            var objects = new List<ObjectInfo>();
            int ind = 0;
            // using (var writer = new StreamWriter(Path.Combine("Data", "Dorothea", "thrombin_unique_set_True.data.new")))
            using (var reader = new StreamReader(Path.Combine("Data", "Train", "thrombin.data")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    // writer.WriteLine(line.Insert(1, "\t"));
                    var dt = new ObjectInfo()
                    {
                        ClassValue = line[0] == 'A' ? 1 : 2,
                        Data = Array.ConvertAll(line.Substring(2).Split(',', StringSplitOptions.RemoveEmptyEntries), x => x == "1" ? 1M : 0M),
                        Index = ind++
                    };

                    objects.Add(dt);
                }
            }
            System.Console.WriteLine(objects[0].Data.Count());
            var newset = new ObjectSet(this.GetType().Name, objects.ToArray(), features.ToArray());
            return newset;
        }
    }
}