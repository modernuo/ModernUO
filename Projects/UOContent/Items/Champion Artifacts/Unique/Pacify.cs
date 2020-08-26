namespace Server.Items
{
    public class Pacify : Pike
    {
        [Constructible]
        public Pacify()
        {
            Hue = 0x835;

            Attributes.SpellChanneling = 1;
            Attributes.AttackChance = 10;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 50;

            WeaponAttributes.HitLeechMana = 100;
            WeaponAttributes.UseBestSkill = 1;
        }

        public Pacify(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094929; // Pacify [Replica]

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
