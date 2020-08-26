namespace Server.Items
{
    public class WaterloggedBoots : BaseShoes
    {
        [Constructible]
        public WaterloggedBoots() : base(0x1711)
        {
            if (Utility.RandomBool())
            {
                // thigh boots
                ItemID = 0x1711;
                Weight = 4.0;
            }
            else
            {
                // boots
                ItemID = 0x170B;
                Weight = 3.0;
            }
        }

        public WaterloggedBoots(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074364; // Waterlogged boots

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }

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
