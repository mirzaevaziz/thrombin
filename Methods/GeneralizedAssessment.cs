using System.Collections.Generic;
using System.Linq;
using thrombin.Models;

namespace thrombin.Methods
{
    public class GeneralizedAssessment
    {
        public static decimal FindNonContiniousFeature(ObjectInfo objectInfo, Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult> weights, IEnumerable<int> activeFeatures)
        {
            var result = 0M;

            foreach (var ft in activeFeatures)
            {
                result += weights[ft].FeatureContribute[objectInfo[ft]];
            }

            return result;
        }
        public static Dictionary<int, decimal> FindNonContiniousFeature(ObjectSet set, IEnumerable<int> activeFeatures)
        {
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

            return FindNonContiniousFeature(set, ftWeight, activeFeatures);
        }

        public static Dictionary<int, decimal> FindNonContiniousFeature(ObjectSet set, Dictionary<int, Criterions.NonContinuousFeatureCriterion.NonContinuousFeatureCriterionResult> featureWeights, IEnumerable<int> activeFeatures)
        {
            var result = new Dictionary<int, decimal>();
            for (int objInd = 0; objInd < set.Objects.Length; objInd++)
                result[objInd] = 0;
            foreach (var key in activeFeatures)
            {
                for (int objInd = 0; objInd < set.Objects.Length; objInd++)
                {
                    result[objInd] += featureWeights[key].FeatureContribute[set.Objects[objInd][key]];
                }
            }

            return result;
        }
    }
}