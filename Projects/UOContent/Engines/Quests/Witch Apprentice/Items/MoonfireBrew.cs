namespace Server.Engines.Quests.Hag
{
    public class MoonfireBrew : Item
    {
        [Constructible]
        public MoonfireBrew() : base(0xF04) => Weight = 1.0;

        public MoonfireBrew(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1055065; // a bottle of magical moonfire brew

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
