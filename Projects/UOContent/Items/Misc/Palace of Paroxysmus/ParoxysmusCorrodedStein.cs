namespace Server.Items
{
    public class ParoxysmusCorrodedStein : Item
    {
        [Constructible]
        public ParoxysmusCorrodedStein() : base(0x9D6)
        {
        }

        public ParoxysmusCorrodedStein(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072083; // Paroxysmus' Corroded Stein

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
