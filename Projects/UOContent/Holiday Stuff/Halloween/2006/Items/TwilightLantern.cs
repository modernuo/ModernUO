namespace Server.Items
{
    public class TwilightLantern : Lantern
    {
        [Constructible]
        public TwilightLantern() => Hue = Utility.RandomBool() ? 244 : 997;

        public TwilightLantern(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Twilight Lantern";

        public override bool AllowEquippedCast(Mobile from) => true;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060482); // Spell Channeling
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
