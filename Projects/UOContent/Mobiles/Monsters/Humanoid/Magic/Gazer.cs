using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Gazer : BaseCreature
    {
        [Constructible]
        public Gazer() : base(AIType.AI_Mage)
        {
            Body = 22;
            BaseSoundID = 377;

            SetStr(96, 125);
            SetDex(86, 105);
            SetInt(141, 165);

            SetHits(58, 75);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.EvalInt, 50.1, 65.0);
            SetSkill(SkillName.Magery, 50.1, 65.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 36;

            PackItem(new Nightshade(4));
        }

        public override string CorpseName => "a gazer corpse";
        public override string DefaultName => "a gazer";

        public override int TreasureMapLevel => 1;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Potions);
        }
    }
}
