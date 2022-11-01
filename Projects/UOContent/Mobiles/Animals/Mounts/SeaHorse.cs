namespace Server.Mobiles
{
    public class SeaHorse : BaseMount
    {
        public override string DefaultName => "a sea horse";

        [Constructible]
        public SeaHorse() : base(0x90, 0x3EB3, AIType.AI_Animal, FightMode.Aggressor)
        {
            InitStats(Utility.Random(50, 30), Utility.Random(50, 30), 10);
            Skills.MagicResist.Base = 25.0 + Utility.RandomDouble() * 5.0;
            Skills.Wrestling.Base = 35.0 + Utility.RandomDouble() * 10.0;
            Skills.Tactics.Base = 30.0 + Utility.RandomDouble() * 15.0;
        }

        public SeaHorse(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a sea horse corpse";

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
