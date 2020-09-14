using Server.Items;

namespace Server.Engines.ConPVP
{
    public enum DuelTeleporterType
    {
        Squares = 6095,
        Buds = 6104,
        Flowers = 6113,
        Spikes = 6122,
        Arrows = 6140,
        Links = 6149
    }

    public class DuelTeleporterAddon : BaseAddon
    {
        [Constructible]
        public DuelTeleporterAddon(DuelTeleporterType type = DuelTeleporterType.Squares)
        {
            var itemID = (int)type;

            AddComponent(new AddonComponent(itemID + 0), -1, -1, 5);
            AddComponent(new AddonComponent(itemID + 1), -1, 0, 5);
            AddComponent(new AddonComponent(itemID + 2), 0, -1, 5);
            AddComponent(new AddonComponent(itemID + 3), -1, +1, 5);
            AddComponent(new AddonComponent(itemID + 4), 0, 0, 5);
            AddComponent(new AddonComponent(itemID + 5), +1, -1, 5);
            AddComponent(new AddonComponent(itemID + 6), 0, +1, 5);
            AddComponent(new AddonComponent(itemID + 7), +1, 0, 5);
            AddComponent(new AddonComponent(itemID + 8), +1, +1, 5);

            AddComponent(new AddonComponent(0x759), -2, -2, 0);
            AddComponent(new AddonComponent(0x75A), +2, +2, 0);
            AddComponent(new AddonComponent(0x75B), -2, +2, 0);
            AddComponent(new AddonComponent(0x75C), +2, -2, 0);

            AddComponent(new AddonComponent(0x751), -1, +2, 0);
            AddComponent(new AddonComponent(0x751), 0, +2, 0);
            AddComponent(new AddonComponent(0x751), +1, +2, 0);

            AddComponent(new AddonComponent(0x752), +2, -1, 0);
            AddComponent(new AddonComponent(0x752), +2, 0, 0);
            AddComponent(new AddonComponent(0x752), +2, +1, 0);

            AddComponent(new AddonComponent(0x753), -1, -2, 0);
            AddComponent(new AddonComponent(0x753), 0, -2, 0);
            AddComponent(new AddonComponent(0x753), +1, -2, 0);

            AddComponent(new AddonComponent(0x754), -2, -1, 0);
            AddComponent(new AddonComponent(0x754), -2, 0, 0);
            AddComponent(new AddonComponent(0x754), -2, +1, 0);
        }

        public DuelTeleporterAddon(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DuelTeleporterType Type
        {
            get
            {
                if (Components.Count > 0)
                {
                    return (DuelTeleporterType)Components[0].ItemID;
                }

                return DuelTeleporterType.Squares;
            }
            set
            {
                for (var i = 0; i < Components.Count && i < 9; ++i)
                {
                    Components[i].ItemID = i + (int)value;
                }
            }
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
}
