using thrombin.Models;

namespace thrombin.Methods
{
    public class NormilizingMinMax
    {
        public static ObjectSet Normalize(ObjectSet set)
        {
            for (int i = 0; i < set.Features.Length; i++)
            {
                if (!set.Features[i].IsContinuous)
                    continue;

                decimal? max_val = null;
                decimal? min_val = null;

                foreach (var item in set.Objects)
                {
                    if (!max_val.HasValue || max_val < item[i])
                        max_val = item[i];
                    if (!min_val.HasValue || min_val > item[i])
                        min_val = item[i];
                }

                if (max_val.Value - min_val.Value == 0) continue;

                foreach (var item in set.Objects)
                {
                    item[i] = (item[i] - min_val.Value) / (max_val.Value - min_val.Value);
                }
            }

            return set;
        }
    }
}