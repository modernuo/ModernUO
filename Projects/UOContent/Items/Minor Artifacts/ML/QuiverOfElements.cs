namespace Server.Items
{
    public class QuiverOfElements : BaseQuiver
    {
        [Constructible]
        public QuiverOfElements()
        {
            Hue = 0xEB;
            WeightReduction = 50;
        }

        public QuiverOfElements(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075040; // Quiver of the Elements

        public override void AlterBowDamage(
            ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
            ref int chaos, ref int direct
        )
        {
            phys = fire = cold = pois = nrgy = direct = 0;
            chaos = 100;
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
