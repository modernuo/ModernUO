namespace Server.Items
{
    public class QuiverOfLightning : ElvenQuiver
    {
        [Constructible]
        public QuiverOfLightning() => Hue = 0x4F9;

        public QuiverOfLightning(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073112; // Quiver of Lightning

        public override void AlterBowDamage(
            out int phys, out int fire, out int cold, out int pois, out int nrgy,
            out int chaos, out int direct
        )
        {
            fire = cold = pois = chaos = direct = 0;
            phys = nrgy = 50;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
