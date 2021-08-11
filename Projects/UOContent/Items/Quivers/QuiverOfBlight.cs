namespace Server.Items
{
    public class QuiverOfBlight : ElvenQuiver
    {
        [Constructible]
        public QuiverOfBlight() => Hue = 0x4F3;

        public QuiverOfBlight(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073111; // Quiver of Blight

        public override void AlterBowDamage(
            out int phys, out int fire, out int cold, out int pois, out int nrgy,
            out int chaos, out int direct
        )
        {
            phys = fire = nrgy = chaos = direct = 0;
            cold = pois = 50;
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
