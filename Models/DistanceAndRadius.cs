using System.Collections.Generic;

namespace thrombin.Models
{
    public class DistanceAndRadius
    {
        public decimal[,] Distances { get; set; }
        public Dictionary<int, decimal> Radiuses { get; set; }
    }
}