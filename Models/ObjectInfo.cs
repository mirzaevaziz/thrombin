using System;

namespace thrombin.Models
{
    public class ObjectInfo
    {
        public int Index { get; set; }
        public int? ClassValue { get; set; }

        public decimal this[int index]
        {
            get { return Data[index]; }
            set { Data[index] = value; }
        }

        public decimal[] Data { get; set; }

        public override string ToString()
        {
            return $"Object {Index} : [{string.Join(", ", Data)}], {ClassValue}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var o = obj as ObjectInfo;
            if (o.Data.Length != Data.Length)
                return false;

            if (ClassValue != ClassValue)
                return false;

            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i] != o.Data[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal bool EqualsByValues(ObjectInfo objectInfo)
        {
            for (int i = 0; i < this.Data.Length; i++)
            {
                if (this[i] != objectInfo[i])
                    return false;
            }

            return true;
        }
    }
}