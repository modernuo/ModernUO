namespace Server.Items
{
    public class RingOfTheVile : GoldRing
    {
        [Constructible]
        public RingOfTheVile()
        {
            Hue = 0x4F7;
            Attributes.BonusDex = 8;
            Attributes.RegenStam = 6;
            Attributes.AttackChance = 15;
            Resistances.Poison = 20;
        }

        public RingOfTheVile(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061102; // Ring of the Vile
        public override int ArtifactRarity => 11;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Hue == 0x4F4)
            {
                Hue = 0x4F7;
            }
        }
    }
}
