using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.Test
{
    public class ThrombinTestSet
    {
        public ObjectSet GetSet()
        {
            var objects = new List<ObjectInfo>();
            int ind = 0;
            var features = Enumerable.Range(0, 139351).Select(s => new Feature() { IsContinuous = false, Name = $"Ft {s:000000}" });

            using (var reader = new StreamReader(Path.Combine("Data", "Test", "Thrombin.testset")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    // writer.WriteLine(line.Insert(1, "\t"));
                    var dt = new ObjectInfo()
                    {
                        Data = Array.ConvertAll(line.Substring(2).Split(',', StringSplitOptions.RemoveEmptyEntries), x => x == "1" ? 1M : 0M),
                        Index = ind++
                    };

                    objects.Add(dt);
                }
            }

            ind = 0;
            using (var reader = new StreamReader(Path.Combine("Data", "Test", "thrombin_test_keys.txt")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    objects[ind++].ClassValue = line[0] == 'A' ? 1 : 2;
                }
            }

            return new ObjectSet(this.GetType().Name, objects.ToArray(), features.ToArray());
        }
    }
}