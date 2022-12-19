using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TerathanMatriarch : BaseCreature
    {
        [Constructible]
        public TerathanMatriarch() : base(AIType.AI_Mage)
        {
            Body = 72;
            BaseSoundID = 599;

            SetStr(316, 405);
            SetDex(96, 115);
            SetInt(366, 455);

            SetHits(190, 243);

            SetDamage(11, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 10000;
            Karma = -10000;

            PackItem(new SpidersSilk(5));
            PackNecroReg(Utility.RandomMinMax(4, 10));
        }

        public override string CorpseName => "a terathan matriarch corpse";
        public override string DefaultName => "a terathan matriarch";

        public override int TreasureMapLevel => 4;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.MedScrolls, 2);
            AddLoot(LootPack.Potions);
        }
    }
}
