namespace Server.Engines.Quests.Doom
{
    public class GrandGrimoire : Item
    {
        [Constructible]
        public GrandGrimoire() : base(0xEFA)
        {
            Weight = 1.0;
            Hue = 0x835;
            Layer = Layer.OneHanded;
            LootType = LootType.Blessed;
        }

        public GrandGrimoire(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1060801; // The Grand Grimoire

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
