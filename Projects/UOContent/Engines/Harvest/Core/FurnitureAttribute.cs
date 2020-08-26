using System;

namespace Server
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FurnitureAttribute : Attribute
    {
        public static bool Check(Item item) => item?.GetType().IsDefined(typeof(FurnitureAttribute), false) == true;
    }
}
