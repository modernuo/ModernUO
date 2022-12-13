using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Doppleganger : BaseCreature
    {
        [Constructible]
        public Doppleganger() : base(AIType.AI_Melee)
        {
            Body = 0x309;
            BaseSoundID = 0x451;

            SetStr(81, 110);
            SetDex(56, 75);
            SetInt(81, 105);

            SetHits(101, 120);

            SetDamage(8, 12);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 75.1, 85.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Wrestling, 80.1, 90.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 55;
        }

        public override string CorpseName => "a doppleganger corpse";
        public override string DefaultName => "a doppleganger";

        public override int Hides => 6;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
