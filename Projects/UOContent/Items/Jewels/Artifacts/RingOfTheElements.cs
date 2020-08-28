namespace Server.Items
{
    public class RingOfTheElements : GoldRing
    {
        [Constructible]
        public RingOfTheElements()
        {
            Hue = 0x4E9;
            Attributes.Luck = 100;
            Resistances.Fire = 16;
            Resistances.Cold = 16;
            Resistances.Poison = 16;
            Resistances.Energy = 16;
        }

        public RingOfTheElements(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061104; // Ring of the Elements
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
        }
    }
}
