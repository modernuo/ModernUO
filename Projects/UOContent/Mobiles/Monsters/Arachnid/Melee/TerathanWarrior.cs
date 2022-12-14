using ModernUO.Serialization;
using Server.Engines.Plants;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TerathanWarrior : BaseCreature
    {
        [Constructible]
        public TerathanWarrior() : base(AIType.AI_Melee)
        {
            Body = 70;
            BaseSoundID = 589;

            SetStr(166, 215);
            SetDex(96, 145);
            SetInt(41, 65);

            SetHits(100, 129);
            SetMana(0);

            SetDamage(7, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 90.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 30;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(3));
            }
        }

        public override string CorpseName => "a terathan warrior corpse";
        public override string DefaultName => "a terathan warrior";

        public override int TreasureMapLevel => 1;
        public override int Meat => 4;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
