using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items
{
    [Anvil]
    [SerializationGenerator(0, false)]
    public partial class AnvilComponent : AddonComponent
    {
        [Constructible]
        public AnvilComponent(int itemID) : base(itemID)
        {
        }
    }

    [Forge]
    [SerializationGenerator(0, false)]
    public partial class ForgeComponent : AddonComponent
    {
        [Constructible]
        public ForgeComponent(int itemID) : base(itemID)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LocalizedAddonComponent : AddonComponent
    {
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _number;

        [Constructible]
        public LocalizedAddonComponent(int itemID, int labelNumber) : base(itemID) => _number = labelNumber;

        public override int LabelNumber => _number;
    }

    [SerializationGenerator(1, false)]
    public partial class AddonComponent : Item, IChoppable
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

        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        public BaseAddon _addon;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        public Point3D _offset;

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;

                if (_addon?.ShareHue == true)
                {
                    _addon.Hue = value;
                }
            }
        }

        public virtual bool NeedsWall => false;
        public virtual Point3D WallPosition => Point3D.Zero;

        public void OnChop(Mobile from)
        {
            if (_addon != null && from.InRange(GetWorldLocation(), 3))
            {
                _addon.OnChop(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override void OnDoubleClick(Mobile from) => _addon?.OnComponentUsed(this, from);

        public override void OnLocationChange(Point3D old)
        {
            if (_addon != null)
            {
                _addon.Location = new Point3D(X - _offset.X, Y - _offset.Y, Z - _offset.Z);
            }
        }

        public override void OnMapChange()
        {
            if (_addon != null)
            {
                _addon.Map = Map;
            }
        }
/*
        public override void GetProperties(IPropertyList list) => _addon?.GetProperties(list);

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list) =>
            _addon?.GetContextMenuEntries(from, list);
*/
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _addon?.Delete();
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            if (version < 1 && Weight == 0)
            {
                Weight = -1;
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            _addon?.OnComponentLoaded(this);
            ApplyLightTo(this);
        }

        public static void ApplyLightTo(Item item)
        {
            if (!item.ItemData.LightSource)
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
