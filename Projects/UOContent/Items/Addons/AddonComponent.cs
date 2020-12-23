using Server.Engines.Craft;

namespace Server.Items
{
    [Anvil]
    public class AnvilComponent : AddonComponent
    {
        [Constructible]
        public AnvilComponent(int itemID) : base(itemID)
        {
        }

        public AnvilComponent(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [Forge]
    public class ForgeComponent : AddonComponent
    {
        [Constructible]
        public ForgeComponent(int itemID) : base(itemID)
        {
        }

        public ForgeComponent(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class LocalizedAddonComponent : AddonComponent
    {
        private int m_LabelNumber;

        [Constructible]
        public LocalizedAddonComponent(int itemID, int labelNumber) : base(itemID) => m_LabelNumber = labelNumber;

        public LocalizedAddonComponent(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Number
        {
            get => m_LabelNumber;
            set
            {
                m_LabelNumber = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => m_LabelNumber;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_LabelNumber);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_LabelNumber = reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class AddonComponent : Item, IChoppable
    {
        private static readonly LightEntry[] m_Entries =
        {
            new(
                LightType.WestSmall,
                1122,
                1123,
                1124,
                1141,
                1142,
                1143,
                1144,
                1145,
                1146,
                2347,
                2359,
                2360,
                2361,
                2362,
                2363,
                2364,
                2387,
                2388,
                2389,
                2390,
                2391,
                2392
            ),
            new(
                LightType.NorthSmall,
                1131,
                1133,
                1134,
                1147,
                1148,
                1149,
                1150,
                1151,
                1152,
                2352,
                2373,
                2374,
                2375,
                2376,
                2377,
                2378,
                2401,
                2402,
                2403,
                2404,
                2405,
                2406
            ),
            new(LightType.Circle300, 6526, 6538, 6571),
            new(LightType.Circle150, 5703, 6587)
        };

        [Constructible]
        public AddonComponent(int itemID) : base(itemID)
        {
            Movable = false;
            ApplyLightTo(this);
        }

        public AddonComponent(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseAddon Addon { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get; set; }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;

                if (Addon?.ShareHue == true)
                {
                    Addon.Hue = value;
                }
            }
        }

        public virtual bool NeedsWall => false;
        public virtual Point3D WallPosition => Point3D.Zero;

        public void OnChop(Mobile from)
        {
            if (Addon != null && from.InRange(GetWorldLocation(), 3))
            {
                Addon.OnChop(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            Addon?.OnComponentUsed(this, from);
        }

        public override void OnLocationChange(Point3D old)
        {
            if (Addon != null)
            {
                Addon.Location = new Point3D(X - Offset.X, Y - Offset.Y, Z - Offset.Z);
            }
        }

        public override void OnMapChange()
        {
            if (Addon != null)
            {
                Addon.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Addon?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Addon);
            writer.Write(Offset);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        Addon = reader.ReadEntity<BaseAddon>();
                        Offset = reader.ReadPoint3D();

                        Addon?.OnComponentLoaded(this);

                        ApplyLightTo(this);

                        break;
                    }
            }

            if (version < 1 && Weight == 0)
            {
                Weight = -1;
            }
        }

        public static void ApplyLightTo(Item item)
        {
            if ((item.ItemData.Flags & TileFlag.LightSource) == 0)
            {
                return; // not a light source
            }

            var itemID = item.ItemID;

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                var entry = m_Entries[i];
                var toMatch = entry.m_ItemIDs;
                var contains = false;

                for (var j = 0; !contains && j < toMatch.Length; ++j)
                {
                    contains = itemID == toMatch[j];
                }

                if (contains)
                {
                    item.Light = entry.m_Light;
                    return;
                }
            }
        }

        private class LightEntry
        {
            public readonly int[] m_ItemIDs;
            public readonly LightType m_Light;

            public LightEntry(LightType light, params int[] itemIDs)
            {
                m_Light = light;
                m_ItemIDs = itemIDs;
            }
        }
    }
}
