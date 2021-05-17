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
        public List<ObjectInfo> GetSet()
        {
            var objects = new List<ObjectInfo>();
            int ind = 0;
            // using (var writer = new StreamWriter(Path.Combine("Data", "Dorothea", "thrombin_unique_set_True.data.new")))
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

            return objects;
        }
    }
}