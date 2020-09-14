using System;
using System.Collections.Generic;

namespace Server.Engines.Craft
{
    public static class CraftCollectionExtensions
    {
        public static int SearchFor(this List<CraftGroup> list, TextDefinition groupName)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var craftGroup = list[i];

                var nameNumber = craftGroup.NameNumber;
                var nameString = craftGroup.NameString;

                if (nameNumber != 0 && nameNumber == groupName.Number ||
                    nameString != null && nameString == groupName.String)
                {
                    return i;
                }
            }

            return -1;
        }

        public static CraftItem SearchForSubclass(this List<CraftItem> list, Type type)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var craftItem = list[i];

                if (craftItem.ItemType == type || type.IsSubclassOf(craftItem.ItemType))
                {
                    return craftItem;
                }
            }

            return null;
        }

        public static CraftItem SearchFor(this List<CraftItem> list, Type type)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var craftItem = list[i];
                if (craftItem.ItemType == type)
                {
                    return craftItem;
                }
            }

            return null;
        }
    }
}
