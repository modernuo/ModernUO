namespace Server.Items
{
    [Flippable(0x27A3, 0x27EE)]
    public class Tessen : BaseBashing
    {
        [Constructible]
        public Tessen() : base(0x27A3)
        {
            Weight = 6.0;
            Layer = Layer.TwoHanded;
        }

        public Tessen(Serial serial) : base(serial)
        {
        }

        public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Block;

        public override int AosStrengthReq => 10;
        public override int AosMinDamage => 10;
        public override int AosMaxDamage => 12;
        public override int AosSpeed => 50;
        public override float MlSpeed => 2.00f;

        public override int OldStrengthReq => 10;
        public override int OldMinDamage => 10;
        public override int OldMaxDamage => 12;
        public override int OldSpeed => 50;

        public override int DefHitSound => 0x232;
        public override int DefMissSound => 0x238;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 60;

        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;

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
