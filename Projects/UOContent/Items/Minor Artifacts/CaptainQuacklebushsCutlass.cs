namespace Server.Items
{
    public class CaptainQuacklebushsCutlass : Cutlass
    {
        [Constructible]
        public CaptainQuacklebushsCutlass()
        {
            Hue = 0x66C;
            Attributes.BonusDex = 5;
            Attributes.AttackChance = 10;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 50;
            WeaponAttributes.UseBestSkill = 1;
        }

        public CaptainQuacklebushsCutlass(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063474;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Attributes.AttackChance == 50)
            {
                Attributes.AttackChance = 10;
            }
        }
    }
}
