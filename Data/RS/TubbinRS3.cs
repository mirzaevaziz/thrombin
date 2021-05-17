using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.RS
{
    public class TubbinRS3 : IObjectSetProvider
    {
        public ObjectSet GetSet()
        {
            var features = Enumerable.Range(0, 3).Select(s => new Models.Feature { IsContinuous = true, Name = $"Ft of Rs {s}" }).ToArray();

            var objects = new List<ObjectInfo>();
            int ind = 0;
            using (var reader = new StreamReader(Path.Combine("Data", "RS", "tubbin3.dat")))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var data = Array.ConvertAll(line.Split('\t', StringSplitOptions.RemoveEmptyEntries), x => decimal.Parse(x, NumberStyles.Float, CultureInfo.InvariantCulture));
                    var dt = new ObjectInfo()
                    {
                        ClassValue = (int)data[3],
                        Data = data.Take(3).ToArray(),
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