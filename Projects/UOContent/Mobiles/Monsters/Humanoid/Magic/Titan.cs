using ModernUO.Serialization;
using Server.Engines.Plants;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Titan : BaseCreature
    {
        [Constructible]
        public Titan() : base(AIType.AI_Mage)
        {
            Body = 76;
            BaseSoundID = 609;

            SetStr(536, 585);
            SetDex(126, 145);
            SetInt(281, 305);

            SetHits(322, 351);

            SetDamage(13, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 85.1, 100.0);
            SetSkill(SkillName.Magery, 85.1, 100.0);
            SetSkill(SkillName.MagicResist, 80.2, 110.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 40.1, 50.0);

            Fame = 11500;
            Karma = -11500;

            VirtualArmor = 40;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(1));
            }
        }

        public override string CorpseName => "a titans corpse";
        public override string DefaultName => "a titan";

        public override int Meat => 4;
        public override Poison PoisonImmune => Poison.Regular;
        public override int TreasureMapLevel => 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.MedScrolls);
        }
    }
}
