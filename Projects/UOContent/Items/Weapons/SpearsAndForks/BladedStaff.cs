namespace Server.Items
{
    [Flippable(0x26BD, 0x26C7)]
    public class BladedStaff : BaseSpear
    {
        [Constructible]
        public BladedStaff() : base(0x26BD) => Weight = 4.0;

        public BladedStaff(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 16;
        public override int AosSpeed => 37;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 14;
        public override int OldMaxDamage => 16;
        public override int OldSpeed => 37;

        public override int InitMinHits => 21;
        public override int InitMaxHits => 110;

        public override SkillName DefSkill => SkillName.Swords;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
