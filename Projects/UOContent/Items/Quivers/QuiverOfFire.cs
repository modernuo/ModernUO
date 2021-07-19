namespace Server.Items
{
    public class QuiverOfFire : ElvenQuiver
    {
        [Constructible]
        public QuiverOfFire() => Hue = 0x4E7;

        public QuiverOfFire(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073109; // quiver of fire

        public override void AlterBowDamage(
            out int phys, out int fire, out int cold, out int pois, out int nrgy,
            out int chaos, out int direct
        )
        {
            cold = pois = nrgy = chaos = direct = 0;
            phys = fire = 50;
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
