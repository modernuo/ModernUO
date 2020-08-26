using System;

namespace Server.Items
{
    public class SpeckledPoisonSac : TransientItem
    {
        [Constructible]
        public SpeckledPoisonSac() : base(0x23A, TimeSpan.FromHours(1)) => LootType = LootType.Blessed;

        public SpeckledPoisonSac(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073133; // Speckled Poison Sac

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
