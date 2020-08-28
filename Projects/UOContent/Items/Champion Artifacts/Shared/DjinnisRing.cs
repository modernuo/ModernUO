namespace Server.Items
{
    public class DjinnisRing : SilverRing
    {
        [Constructible]
        public DjinnisRing()
        {
            Attributes.BonusInt = 5;
            Attributes.SpellDamage = 10;
            Attributes.CastSpeed = 2;
        }

        public DjinnisRing(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094927; // Djinni's Ring [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
