using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace thrombin.Criterions
{
    public class IntervalCriterion
    {
        public class IntervalCriterionParameter
        {
            public int ObjectIndex { get; set; }
            public decimal Distance { get; set; }
            public decimal ClassValue { get; set; }
        }

        public class IntervalCounter
        {
            public int Index { get; set; }
            public int ClassCount { get; set; }
            public int NonClassCount { get; set; }
            public decimal Value { get; internal set; }
        }

        public class IntervalCriterionResult
        {
            public int ObjectIndexStart { get; set; }
            public int ObjectIndexEnd { get; set; }
            public decimal ObjectValueStart { get; set; }
            public decimal ObjectValueEnd { get; set; }
            public decimal Value { get; set; }
            public int PositionIndexStart { get; internal set; }
            public int PositionIndexEnd { get; internal set; }
            public decimal FunctionValue { get; internal set; }
            public int ClassObjectCount { get; internal set; }
            public int NonClassObjectCount { get; internal set; }

            public override string ToString()
            {
                return $"d1={ClassObjectCount}, d2 = {NonClassObjectCount}, Value = {Value:0.000000}, FunctionValue = {FunctionValue:0.000000} [{ObjectValueStart:0.000000}, {ObjectValueEnd:0.000000}]{Environment.NewLine}";
            }
        }

        public static IEnumerable<IntervalCriterionResult> Find(IEnumerable<IntervalCriterionParameter> objects, decimal classValue)
        {
            var result = new List<IntervalCriterionResult>();

            int k = 0, ck = 0;

            var sorted = objects.OrderBy(o => o.Distance).Select(s => new IntervalCounter
            {
                Index = s.ObjectIndex,
                ClassCount = (s.ClassValue == classValue) ? ++k : k,
                NonClassCount = (s.ClassValue != classValue) ? ++ck : ck,
                Value = s.Distance
            }).ToList();
            Devide_to_Interval(sorted, 0, k + ck - 1, k, ck, result);
            return result;
        }

        static void Devide_to_Interval(IList<IntervalCounter> objects, int start_p, int end_p, int classCount, int nonClassCount, IList<IntervalCriterionResult> result)
        {
            decimal tekushiy;
            int pos_st, pos_end;
            pos_st = start_p;
            pos_end = end_p;

            // if (pos_st == pos_end)
            // {
            //     result.Add(new IntervalCriterionResult()
            //     {
            //         ObjectIndexEnd = start_p,
            //         ObjectIndexStart = end_p,
            //         Value = 0
            //     });

            //     return;
            // }
            var max = new IntervalCriterionResult() { Value = -1 };
            decimal div = classCount * nonClassCount;
            for (int i = start_p; i <= end_p; i++)
            {
                int k = 0, ck = 0;
                if (i > 0)
                {
                    k = objects[i - 1].ClassCount;
                    ck = objects[i - 1].NonClassCount;
                }
                if (i != start_p && objects[i].Value == objects[i - 1].Value) continue;
                for (int j = i; j <= end_p; j++)
                {
                    if (j < end_p && objects[j].Value == objects[j + 1].Value) continue;

                    tekushiy = Math.Abs(((objects[j].ClassCount - k) * nonClassCount - (objects[j].NonClassCount - ck) * classCount) / div);

                    if (Math.Round(tekushiy, 6) >= Math.Round(max.Value, 6))
                    {
                        pos_st = i;
                        pos_end = j;
                        max.PositionIndexStart = pos_st;
                        max.PositionIndexEnd = pos_end;
                        max.ObjectIndexStart = objects[pos_st].Index;
                        max.ObjectIndexEnd = objects[pos_end].Index;
                        max.ObjectValueStart = objects[pos_st].Value;
                        max.ObjectValueEnd = objects[pos_end].Value;
                        max.Value = tekushiy;
                        max.ClassObjectCount = (objects[j].ClassCount - k);
                        max.NonClassObjectCount = (objects[j].NonClassCount - ck);
                        max.FunctionValue = (max.ClassObjectCount / (decimal)classCount) / (((max.ClassObjectCount * nonClassCount + max.NonClassObjectCount * classCount) / div));
                    }
                }
            }
            result.Add(max);

            if (start_p < pos_st)
                Devide_to_Interval(objects, start_p, pos_st - 1, classCount, nonClassCount, result);

            if (pos_end < end_p)
                Devide_to_Interval(objects, pos_end + 1, end_p, classCount, nonClassCount, result);
        }
    }
}