using System;
using Server.Items;

namespace Server.Commands;

public readonly record struct LeanMetadata(int ItemID, int Hue, string Name, int? Cliloc);

public static class ObjectIntrospection
{
    public static LeanMetadata ExtractLean(Type type)
    {
        if (type.IsAssignableTo(typeof(Item)))
        {
            var item = type.CreateInstance<Item>();
            try
            {
                var itemID = item.ItemID;
                if (item is BaseAddon addon && addon.Components.Count == 1)
                {
                    itemID = addon.Components[0].ItemID;
                }

                if (itemID > TileData.MaxItemValue)
                {
                    itemID = 1;
                }

                var hue = item.Hue & 0x7FFF;
                hue = (hue & 0x4000) != 0 ? 0 : hue;

                var cliloc = item.LabelNumber > 0 ? item.LabelNumber : (int?)null;
                var name = item.Name ?? (cliloc.HasValue ? Server.Localization.GetText(cliloc.Value) : null);

                return new LeanMetadata(itemID, hue, name, cliloc);
            }
            finally
            {
                item.Delete();
            }
        }

        if (type.IsAssignableTo(typeof(Mobile)))
        {
            var m = type.CreateInstance<Mobile>();
            try
            {
                var itemID = ShrinkTable.Lookup(m, 1);
                var hue = m.Hue & 0x7FFF;
                hue = (hue & 0x4000) != 0 ? 0 : hue;
                return new LeanMetadata(itemID, hue, m.Name, null);
            }
            finally
            {
                m.Delete();
            }
        }

        throw new ArgumentException($"{type} is neither Item nor Mobile.", nameof(type));
    }
}
