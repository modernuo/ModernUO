namespace Server.Items
{
    public class MonsterousInterredGrizzleMaggots : Item
    {
        [Constructible]
        public MonsterousInterredGrizzleMaggots() : base(0x2633)
        {
        }

        public MonsterousInterredGrizzleMaggots(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075090; // Monsterous Interred Grizzle Maggots

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
