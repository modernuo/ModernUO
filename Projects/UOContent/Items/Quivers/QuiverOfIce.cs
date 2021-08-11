namespace Server.Items
{
    public class QuiverOfIce : ElvenQuiver
    {
        [Constructible]
        public QuiverOfIce() => Hue = 0x4ED;

        public QuiverOfIce(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073110; // quiver of ice

        public override void AlterBowDamage(
            out int phys, out int fire, out int cold, out int pois, out int nrgy,
            out int chaos, out int direct
        )
        {
            fire = pois = nrgy = chaos = direct = 0;
            phys = cold = 50;
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
