using System.Collections.Generic;
using System.Linq;

namespace thrombin.Criterions
{
    public class FirstCriterion
    {
        public class FirstCriterionParameter
        {
            public int ObjectIndex { get; set; }
            public decimal Distance { get; set; }
            public decimal ClassValue { get; set; }
        }
        public class FirstCriterionResult
        {
            public int ObjectIndex { get; set; }
            public decimal Distance { get; set; }
            public decimal Value { get; set; }

            public override string ToString()
            {
                return $"ObjectIndex = {ObjectIndex}, Distance = {Distance}, Value = {Value}";
            }
        }
        public static FirstCriterionResult Find(IEnumerable<FirstCriterionParameter> objects, decimal classValue)
        {
            FirstCriterionResult result = null;

            var classCount = objects.Count(w => w.ClassValue == classValue);
            var nonClassCount = objects.Count(w => w.ClassValue != classValue);

            decimal denominator1 = classCount * (classCount - 1) + nonClassCount * (nonClassCount - 1);
            decimal denominator2 = 2 * classCount * nonClassCount;

            var sorted = objects.OrderBy(o => o.Distance).ToArray();

            int u11 = 0, u12 = 0, u21 = 0, u22 = 0;
            for (int i = 0; i < sorted.Length; i++)
            {
                var obj = sorted[i];
                if (obj.ClassValue == classValue)
                {
                    u11++;
                }
                else
                {
                    u21++;
                }
                u12 = classCount - u11;
                u22 = nonClassCount - u21;

                if (i != sorted.Length - 1 && sorted[i + 1].Distance == obj.Distance)
                    continue;

                var val1 = (u11 * (u11 - 1) + u21 * (u21 - 1) + u12 * (u12 - 1) + u22 * (u22 - 1)) / denominator1;
                var val2 = (u11 * (nonClassCount - u21) + u21 * (classCount - u11) + u12 * (nonClassCount - u22) + u22 * (
                classCount - u12)) / denominator2;
                var r = val1 * val2;

                if (result == null || result.Value < r)
                {
                    result = new FirstCriterionResult()
                    {
                        Distance = obj.Distance,
                        ObjectIndex = obj.ObjectIndex,
                        Value = r
                    };
                }
            }

            return result;
        }
    }
}