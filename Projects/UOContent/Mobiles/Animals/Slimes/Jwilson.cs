namespace Server.Mobiles
{
    public class Jwilson : BaseCreature
    {
        [Constructible]
        public Jwilson() : base(AIType.AI_Melee)
        {
            Hue = Utility.RandomList(0x89C, 0x8A2, 0x8A8, 0x8AE);
            Body = 0x33;
            VirtualArmor = 8;

            InitStats(Utility.Random(22, 13), Utility.Random(16, 6), Utility.Random(16, 5));

            Skills.Wrestling.Base = Utility.Random(24, 17);
            Skills.Tactics.Base = Utility.Random(18, 14);
            Skills.MagicResist.Base = Utility.Random(15, 6);
            Skills.Poisoning.Base = Utility.Random(31, 20);

            Fame = Utility.Random(0, 1249);
            Karma = Utility.Random(0, -624);
        }

        public Jwilson(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a jwilson corpse";
        public override string DefaultName => "a jwilson";

        public override int GetAngerSound() => 0x1C8;

        public override int GetIdleSound() => 0x1C9;

        public override int GetAttackSound() => 0x1CA;

        public override int GetHurtSound() => 0x1CB;

        public override int GetDeathSound() => 0x1CC;

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
