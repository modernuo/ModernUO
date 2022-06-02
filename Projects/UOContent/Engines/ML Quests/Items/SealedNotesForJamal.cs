namespace Server.Items
{
    public class SealedNotesForJamal : Item
    {
        [Constructible]
        public SealedNotesForJamal() : base(0xEF9) => LootType = LootType.Blessed;

        public SealedNotesForJamal(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074998; // Sealed Notes For Jamal
        public override double DefaultWeight => 1.0;

        public override bool Nontransferable => true;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);
            AddQuestItemProperty(list);
        }

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
