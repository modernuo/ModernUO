namespace Server.Items
{
    public class AquariumMessage : MessageInABottle
    {
        [Constructible]
        public AquariumMessage()
        {
        }

        public AquariumMessage(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073894; // Message in a Bottle

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
