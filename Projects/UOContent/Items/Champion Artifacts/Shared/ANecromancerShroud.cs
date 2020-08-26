namespace Server.Items
{
    public class ANecromancerShroud : Robe
    {
        [Constructible]
        public ANecromancerShroud() => Hue = 0x455;

        public ANecromancerShroud(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094913; // A Necromancer Shroud [Replica]

        public override int BaseColdResistance => 5;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
