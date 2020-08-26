namespace Server.Items
{
    [Flippable(0x13FB, 0x13FA)]
    public class LargeBattleAxe : BaseAxe
    {
        [Constructible]
        public LargeBattleAxe() : base(0x13FB) => Weight = 6.0;

        public LargeBattleAxe(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
        public override WeaponAbility SecondaryAbility => WeaponAbility.BleedAttack;

        public override int AosStrengthReq => 80;
        public override int AosMinDamage => 16;
        public override int AosMaxDamage => 17;
        public override int AosSpeed => 29;
        public override float MlSpeed => 3.75f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 6;
        public override int OldMaxDamage => 38;
        public override int OldSpeed => 30;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 70;

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
