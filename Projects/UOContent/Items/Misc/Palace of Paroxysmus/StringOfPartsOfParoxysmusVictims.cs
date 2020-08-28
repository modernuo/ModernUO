namespace Server.Items
{
    public class StringOfPartsOfParoxysmusVictims : Item
    {
        [Constructible]
        public StringOfPartsOfParoxysmusVictims() : base(0xFD2)
        {
        }

        public StringOfPartsOfParoxysmusVictims(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072082; // String of Parts of Paroxysmus' Victims

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
