namespace Server.Items
{
    public class SignedTuitionReimbursementForm : Item
    {
        [Constructible]
        public SignedTuitionReimbursementForm() : base(0x14F0) => LootType = LootType.Blessed;

        public SignedTuitionReimbursementForm(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074614; // Signed Tuition Reimbursement Form

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
