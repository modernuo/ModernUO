namespace Server.Items
{
    public class QuiverOfRage : BaseQuiver
    {
        [Constructible]
        public QuiverOfRage()
        {
            Hue = 0x24C;

            WeightReduction = 25;
            DamageIncrease = 10;
        }

        public QuiverOfRage(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075038; // Quiver of Rage

        public override void AlterBowDamage(
            out int phys, out int fire, out int cold, out int pois, out int nrgy,
            out int chaos, out int direct
        )
        {
            chaos = direct = 0;
            phys = fire = cold = pois = nrgy = 20;
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
