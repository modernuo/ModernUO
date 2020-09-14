namespace Server.Items
{
    public class StaffOfTheMagi : BlackStaff
    {
        [Constructible]
        public StaffOfTheMagi()
        {
            Hue = 0x481;
            WeaponAttributes.MageWeapon = 30;
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = 1;
            Attributes.WeaponDamage = 50;
        }

        public StaffOfTheMagi(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061600; // Staff of the Magi
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = cold = pois = chaos = direct = 0;
            nrgy = 100;
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

            if (WeaponAttributes.MageWeapon == 0)
            {
                WeaponAttributes.MageWeapon = 30;
            }

            if (ItemID == 0xDF1)
            {
                ItemID = 0xDF0;
            }
        }
    }
}
