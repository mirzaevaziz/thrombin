using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.RS
{
    public class ThrombinRS5 : IObjectSetProvider
    {
        public ObjectSet GetSet()
        {
            var features = Enumerable.Range(0, 5).Select(s => new Models.Feature { IsContinuous = true, Name = $"Ft of Rs {s}" }).ToArray();

            var objects = new List<ObjectInfo>();
            int ind = 0;
            using (var reader = new StreamReader(Path.Combine("Data", "RS", "new rs set.uu")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var dt = new ObjectInfo()
                    {
                        ClassValue = line[0] == '1' ? 1 : 2,
                        Data = Array.ConvertAll(line.Substring(2).Split('\t', StringSplitOptions.RemoveEmptyEntries), x => decimal.Parse(x, CultureInfo.InvariantCulture)),
                        Index = ind++
                    };

                    objects.Add(dt);
                }
            }

            var newset = new ObjectSet(this.GetType().Name, objects.ToArray(), features);
            return newset;
        }
    }
}