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
            public int ClassValue { get; set; }
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

        public class IntervalInfo
        {
            public int Index { get; set; }
            public decimal DistanceLeft { get; set; }
            public decimal DistanceRight { get; set; }
            public int ObjectLeftIndex { get; set; }
            public int ObjectRightIndex { get; set; }
            public Dictionary<int, int> ClassObjects { get; set; }

            public bool IsInInterval(decimal value)
            {
                if (DistanceLeft == DistanceRight && value == DistanceLeft)
                    return true;

                return DistanceLeft < value && value <= DistanceRight;
            }

            public override string ToString()
            {
                return $"{{#{Index}. Object Index({ObjectLeftIndex}, {ObjectRightIndex}], Distance({DistanceLeft}, {DistanceRight}], ObjectCount({string.Join(";", ClassObjects?.Select(s => $"{s.Key}={s.Value}"))})}}";
            }
        }

        public class FirstCriterionResultV2
        {
            public IList<IntervalInfo> Intervals { get; set; }
            public decimal Value { get; set; }

            public override string ToString()
            {
                return $"Value = {Value:0.0000000}, Intervals = {string.Join(", ", Intervals)}";
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
            for (int i = 0; i < sorted.Length - 1; i++)
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


        public static FirstCriterionResultV2 Find(IEnumerable<FirstCriterionParameter> objects)
        {
            FirstCriterionResultV2 result = null;

            var classValues = objects.Select(s => s.ClassValue).Distinct().ToArray();
            var classCount = classValues.Count();
            var objectsCount = objects.Count();

            objects = objects.OrderBy(o => o.Distance).ToArray();

            var d = new int[classCount, objectsCount + 1];

            for (int i = 0; i < objectsCount; i++)
            {
                for (int j = 0; j < classCount; j++)
                {
                    if (objects.ElementAt(i).ClassValue == classValues[j])
                    {
                        d[j, i + 1] = d[j, i] + 1;
                    }
                    else
                    {
                        d[j, i + 1] = d[j, i];
                    }
                }
            }

            decimal denominator1 = 0, denominator2 = 0;
            for (int i = 0; i < classCount; i++)
            {
                denominator1 += d[i, objectsCount] * (d[i, objectsCount] - 1);
                denominator2 += d[i, objectsCount] * (objectsCount - d[i, objectsCount]);
            }

            //intervallarni o'stirish uchun
            var intervals = new int[classCount + 1];
            //ikki hcekkasini belgilaymiz
            intervals[0] = -1;
            intervals[classCount] = objectsCount - 1;
            for (int i = 1; i < classCount; i++)
            {
                intervals[i] = i - 1;
            }
            decimal maxValue = -1;
            while (true)
            {

                bool hasUniqueValues = true;
                for (int i = 1; i < classCount; i++)
                {
                    if (objects.ElementAt(intervals[i]).Distance == objects.ElementAt(intervals[i] + 1).Distance)
                    {
                        hasUniqueValues = false;
                        break;
                    }
                }
                if (hasUniqueValues)
                {
                    decimal val1 = 0, val2 = 0;
                    for (int p = 1; p <= classCount; p++)
                    {
                        for (int i = 0; i < classCount; i++)
                        {
                            var u_i_p = d[i, intervals[p] + 1] - d[i, intervals[p - 1] + 1];


                            val1 += u_i_p * (u_i_p - 1);

                            var s = 0;
                            for (int j = 0; j < classCount; j++)
                            {
                                var u_j_p = d[j, intervals[p] + 1] - d[j, intervals[p - 1] + 1];
                                s += u_j_p;

                            }
                            val2 += u_i_p * (objectsCount - d[i, objectsCount] - s + u_i_p);
                        }
                    }
                    var r = (val1 / denominator1) * (val2 / denominator2);
                    if (maxValue < r)
                    {
                        maxValue = r;
                        result = new FirstCriterionResultV2()
                        {
                            Value = r,
                            Intervals = new List<IntervalInfo>()
                        };

                        for (int i = 1; i <= classCount; i++)
                        {
                            result.Intervals.Add(new IntervalInfo
                            {
                                Index = i - 1,
                                ObjectLeftIndex = (i == 1) ? objects.ElementAt(0).ObjectIndex : objects.ElementAt(intervals[i - 1]).ObjectIndex,
                                ObjectRightIndex = objects.ElementAt(intervals[i]).ObjectIndex,
                                DistanceLeft = (i == 1) ? objects.ElementAt(0).Distance : objects.ElementAt(intervals[i - 1]).Distance,
                                DistanceRight = objects.ElementAt(intervals[i]).Distance,
                                ClassObjects = new Dictionary<int, int>()
                            });
                            for (int j = 0; j < classCount; j++)
                            {
                                result.Intervals[i - 1].ClassObjects[classValues[j]] = d[j, intervals[i] + 1] - d[j, intervals[i - 1] + 1];
                            }
                        }
                    }
                }

                bool wasChange = false;
                for (int i = 1; i < classCount; i++)
                {
                    if (intervals[classCount - i] != objectsCount - i - 1)
                    {
                        wasChange = true;
                        intervals[classCount - i]++;
                        for (int j = 1; j < i; j++)
                        {
                            intervals[classCount - i + j] = intervals[classCount - i + j - 1] + 1;
                        }
                        break;
                    }
                }
                if (!wasChange)
                    break;
            }

            return result;
        }


    }
}