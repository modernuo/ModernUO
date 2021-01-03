using System;

namespace Server
{
    public class FeatureFlag<T> where T : Item
    {
        public Type Type { get; set; }
    }
}
