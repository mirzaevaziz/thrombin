using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace thrombin.Data;
public class HeartDataSetProvider
{
    public static Models.ObjectSet ReadDataSet(Helpers.Logger logger)
    {
        var features = new List<Models.Feature>();
        using (var header = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "names.data")))
        {
            while (!header.EndOfStream)
            {
                var line = header.ReadLine().Split('|', StringSplitOptions.TrimEntries);
                if (line.Length > 1)
                {
                    features.Add(new Models.Feature
                    {
                        IsContinuous = line[0] == "1",
                        Name = line[1]
                    });
                }
            }
        }

        foreach (var item in features)
        {
            logger.WriteLine("Set info", item.ToString());
        }

        var featureValues = new Dictionary<int, Dictionary<string, int>>();
        var objectList = new List<Models.ObjectInfo>();

        using (var data = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "heart_2020_cleaned.csv")))
        {
            while (!data.EndOfStream)
            {
                var line = data.ReadLine().Split(',', StringSplitOptions.TrimEntries);
                if (line.Length != features.Count)
                    continue;

                var obj = new Models.ObjectInfo();
                obj.Data = new decimal[features.Count];
                for (int i = 0; i < features.Count; i++)
                {
                    if (features[i].IsContinuous)
                    {
                        obj.Data[i] = decimal.Parse(line[i]);
                    }
                    else
                    {
                        if (!(featureValues.ContainsKey(i) && featureValues[i].ContainsKey(line[i])))
                        {
                            if (!featureValues.ContainsKey(i))
                                featureValues[i] = new Dictionary<string, int>();
                            featureValues[i][line[i]] = featureValues[i].Values.Count;
                        }
                        obj.Data[i] = featureValues[i][line[i]];
                    }
                }
                objectList.Add(obj);
            }
        }

        foreach (var ft in featureValues)
        {
            logger.WriteLine("Set info", $"Feature {features[ft.Key].Name} values:");
            foreach (var ftVal in ft.Value)
            {
                logger.WriteLine("Set info", $"\t{ftVal.Value} = {ftVal.Key}");
            }
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].ClassValue = (int)objectList[i].Data[0];
            objectList[i].Data = objectList[i].Data.Skip(1).ToArray();
        }

        var set = new Models.ObjectSet("Heart Disease Prediction", objectList.ToArray(), features.Skip(1).ToArray(), featureValues[0]["Yes"]);
        logger.WriteLine("Set info", set.ToString(), true);

        return set;
    }


    public static Models.ObjectSet ReadDataSetUnique(Helpers.Logger logger)
    {
        var features = new List<Models.Feature>();
        using (var header = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "names.data")))
        {
            while (!header.EndOfStream)
            {
                var line = header.ReadLine().Split('|', StringSplitOptions.TrimEntries);
                if (line.Length > 1)
                {
                    features.Add(new Models.Feature
                    {
                        IsContinuous = line[0] == "1",
                        Name = line[1]
                    });
                }
            }
        }

        foreach (var item in features)
        {
            logger.WriteLine("Set info", item.ToString());
        }

        var similars = new HashSet<int>();
        using (var file = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "Similar indexes.txt")))
        {
            while (!file.EndOfStream)
            {
                similars.Add(int.Parse(file.ReadLine()));
            }
        }

        var featureValues = new Dictionary<int, Dictionary<string, int>>();
        var objectList = new List<Models.ObjectInfo>();

        int ind = -1;
        using (var data = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Data", "Heart Disease Prediction", "heart_2020_cleaned.csv")))
        {
            while (!data.EndOfStream)
            {
                var line = data.ReadLine().Split(',', StringSplitOptions.TrimEntries);
                if (line.Length != features.Count)
                    continue;

                ind++;
                if (similars.Contains(ind)) continue;

                var obj = new Models.ObjectInfo();
                obj.Data = new decimal[features.Count];
                for (int i = 0; i < features.Count; i++)
                {
                    if (features[i].IsContinuous)
                    {
                        obj.Data[i] = decimal.Parse(line[i], System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        if (!(featureValues.ContainsKey(i) && featureValues[i].ContainsKey(line[i])))
                        {
                            if (!featureValues.ContainsKey(i))
                                featureValues[i] = new Dictionary<string, int>();
                            featureValues[i][line[i]] = featureValues[i].Values.Count;
                        }
                        obj.Data[i] = featureValues[i][line[i]];
                    }
                }
                objectList.Add(obj);
            }
        }

        foreach (var ft in featureValues)
        {
            logger.WriteLine("Set info", $"Feature {features[ft.Key].Name} values:");
            foreach (var ftVal in ft.Value)
            {
                logger.WriteLine("Set info", $"\t{ftVal.Value} = {ftVal.Key}");
            }
        }

        for (int i = 0; i < objectList.Count; i++)
        {
            objectList[i].ClassValue = (int)objectList[i].Data[0];
            objectList[i].Data = objectList[i].Data.Skip(1).ToArray();
        }

        var set = new Models.ObjectSet("Heart Disease Prediction", objectList.ToArray(), features.Skip(1).ToArray(), featureValues[0]["Yes"]);
        logger.WriteLine("Set info", set.ToString(), true);

        return set;
    }
}