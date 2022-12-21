using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SilverSteed : BaseMount
    {
        public override string DefaultName => "a silver steed";

        [Constructible]
        public SilverSteed() : base(0x75, 0x3EA8, AIType.AI_Animal, FightMode.Aggressor)
        {
            InitStats(Utility.Random(50, 30), Utility.Random(50, 30), 10);
            Skills.MagicResist.Base = 25.0 + Utility.RandomDouble() * 5.0;
            Skills.Wrestling.Base = 35.0 + Utility.RandomDouble() * 10.0;
            Skills.Tactics.Base = 30.0 + Utility.RandomDouble() * 15.0;

            ControlSlots = 1;
            Tamable = true;
            MinTameSkill = 103.1;
        }

        public override string CorpseName => "a silver steed corpse";
    }
}
