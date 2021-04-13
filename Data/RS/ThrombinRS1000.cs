using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using thrombin.Interfaces;
using thrombin.Models;

namespace thrombin.Data.RS
{
    public class ThrombinRS1000 : IObjectSetProvider
    {
        public ObjectSet GetSet()
        {
            var features = new List<Feature>();

            using (var reader = new StreamReader(Path.Combine("Data", "RS", "thrombin_rs_set_1000.txt.features")))
            {
                reader.ReadLine();
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Split('|');
                    features.Add(new Feature()
                    {
                        // Index = i++,
                        IsContinuous = Convert.ToBoolean(line[2]),
                        Name = line[3]
                    });
                }
            }

            var objects = new List<ObjectInfo>();
            int ind = 0;
            using (var reader = new StreamReader(Path.Combine("Data", "RS", "thrombin_rs_set_1000.txt")))
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

            var newset = new ObjectSet(this.GetType().Name, objects.ToArray(), features.ToArray());
            return newset;
        }
    }
}