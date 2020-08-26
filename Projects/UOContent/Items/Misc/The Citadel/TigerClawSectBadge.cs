namespace Server.Items
{
    public class TigerClawSectBadge : Item
    {
        [Constructible]
        public TigerClawSectBadge() : base(0x23D) => LootType = LootType.Blessed;

        public TigerClawSectBadge(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073140; // A Tiger Claw Sect Badge

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
