using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Methods
{
    public class GeneralizedAssessment
    {
        public static decimal FindNonContiniousFeature(ObjectInfo objectInfo, Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult> weights)
        {
            var result = 0M;

            foreach (var ft in weights.Keys)
            {
                result += weights[ft].FeatureContribute[objectInfo[ft]];
            }

            return result;
        }
        public static Dictionary<int, decimal> FindNonContiniousFeature(ObjectSet set, IEnumerable<int> activeFeatures)
        {
            var result = new Dictionary<int, decimal>();
            var ftWeight = new Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult>();
            foreach (var i in activeFeatures)
            {
                if (set.Features[i].IsContinuous)
                    continue;

                ftWeight[i] = Criterions.NonContinuousFeatureCriterion.Find(set.Objects.Select(s => new Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionParameter()
                {
                    ObjectIndex = s.Index,
                    ClassValue = s.ClassValue.Value,
                    FeatureValue = s[i]
                }), set.ClassValue);
            }

            for (int objInd = 0; objInd < set.Objects.Length; objInd++)
                result[objInd] = 0;
            foreach (var key in ftWeight.Keys)
            {
                System.Console.WriteLine($"Ft{key}: v={ftWeight[key].Value}");
                foreach (var item in ftWeight[key].FeatureContribute)
                {
                    System.Console.WriteLine($"\tm({item.Key}) = {item.Value}");
                }
                for (int objInd = 0; objInd < set.Objects.Length; objInd++)
                {
                    result[objInd] += ftWeight[key].FeatureContribute[set.Objects[objInd][key]];
                }
            }

            foreach (var ga in result)
            {
                System.Console.WriteLine($"Obj[{ga.Key:000000}]={ga.Value}");
            }

            return result;
        }
    }
}