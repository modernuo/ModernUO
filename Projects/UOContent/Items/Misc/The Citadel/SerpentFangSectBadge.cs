namespace Server.Items
{
    public class SerpentFangSectBadge : Item
    {
        [Constructible]
        public SerpentFangSectBadge() : base(0x23C) => LootType = LootType.Blessed;

        public SerpentFangSectBadge(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073139; // A Serpent Fang Sect Badge

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
