namespace Server.Engines.Quests.Doom
{
    public class ChylothShroud : Item
    {
        [Constructible]
        public ChylothShroud() : base(0x204E)
        {
            Hue = 0x846;
            Layer = Layer.OuterTorso;
        }

        public ChylothShroud(Serial serial) : base(serial)
        {
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
