namespace Server.Factions
{
    public class FactionTrapRemovalKit : Item
    {
        [Constructible]
        public FactionTrapRemovalKit() : base(7867)
        {
            LootType = LootType.Blessed;
            Charges = 25;
        }

        public FactionTrapRemovalKit(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges { get; set; }

        public override int LabelNumber => 1041508; // a faction trap removal kit

        public void ConsumeCharge(Mobile consumer)
        {
            --Charges;

            if (Charges <= 0)
            {
                Delete();

                consumer?.SendLocalizedMessage(1042531); // You have used all of the parts in your trap removal kit.
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            // NOTE: OSI does not list uses remaining; intentional difference
            list.Add(1060584, Charges); // uses remaining: ~1_val~
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.WriteEncodedInt(Charges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Charges = reader.ReadEncodedInt();
                        break;
                    }
                case 0:
                    {
                        Charges = 25;
                        break;
                    }
            }
        }
    }
}
