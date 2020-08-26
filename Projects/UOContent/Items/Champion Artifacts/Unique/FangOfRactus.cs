namespace Server.Items
{
    public class FangOfRactus : Kryss
    {
        [Constructible]
        public FangOfRactus()
        {
            Hue = 0x117;

            Attributes.SpellChanneling = 1;
            Attributes.AttackChance = 5;
            Attributes.DefendChance = 5;
            Attributes.WeaponDamage = 35;

            WeaponAttributes.HitPoisonArea = 20;
            WeaponAttributes.ResistPoisonBonus = 15;
        }

        public FangOfRactus(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094892; // Fang of Ractus [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

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
