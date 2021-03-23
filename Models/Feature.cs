namespace thrombin.Models
{
    public class Feature
    {
        public bool IsContinuous { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"Feature{{\"{Name}\" is_continuous={IsContinuous}}}";
        }
    }
}