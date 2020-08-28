namespace Server.Items
{
    public class ColdBlood : Cleaver
    {
        [Constructible]
        public ColdBlood()
        {
            Hue = 0x4F2;

            Attributes.WeaponSpeed = 40;

            Attributes.BonusHits = 6;
            Attributes.BonusStam = 6;
            Attributes.BonusMana = 6;
        }

        public ColdBlood(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1070818; // Cold Blood

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            cold = 100;

            fire = phys = pois = nrgy = chaos = direct = 0;
        }

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
