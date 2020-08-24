using System;

namespace Server.Engines.Craft
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CraftItemIDAttribute : Attribute
    {
        public CraftItemIDAttribute(int itemID) => ItemID = itemID;

        public int ItemID { get; }
    }
}
