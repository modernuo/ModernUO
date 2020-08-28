namespace Server.Items
{
    public class TheMostKnowledgePerson : BaseOuterTorso
    {
        [Constructible]
        public TheMostKnowledgePerson() : base(0x2684)
        {
            Hue = 0x117;
            StrRequirement = 0;

            Attributes.BonusHits = 3 + Utility.RandomMinMax(0, 2);
        }

        public TheMostKnowledgePerson(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094893; // The Most Knowledge Person [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override bool CanBeBlessed => false;

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
