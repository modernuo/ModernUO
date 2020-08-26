namespace Server.Items
{
    [Flippable(0xEC4, 0xEC5)]
    public class SkinningKnife : BaseKnife
    {
        [Constructible]
        public SkinningKnife() : base(0xEC4) => Weight = 1.0;

        public SkinningKnife(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.ShadowStrike;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

        public override int AosStrengthReq => 5;
        public override int AosMinDamage => 9;
        public override int AosMaxDamage => 11;
        public override int AosSpeed => 49;
        public override float MlSpeed => 2.25f;

        public override int OldStrengthReq => 5;
        public override int OldMinDamage => 1;
        public override int OldMaxDamage => 10;
        public override int OldSpeed => 40;

        public override int InitMinHits => 31;
        public override int InitMaxHits => 40;

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
