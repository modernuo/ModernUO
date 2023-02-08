using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class PoisonElemental : BaseCreature
    {
        [Constructible]
        public PoisonElemental() : base(AIType.AI_Mage)
        {
            Body = 162;
            BaseSoundID = 263;

            SetStr(426, 515);
            SetDex(166, 185);
            SetInt(361, 435);

            SetHits(256, 309);

            SetDamage(12, 18);

            SetDamageType(ResistanceType.Physical, 10);
            SetDamageType(ResistanceType.Poison, 90);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 80.1, 95.0);
            SetSkill(SkillName.Magery, 80.1, 95.0);
            SetSkill(SkillName.Meditation, 80.2, 120.0);
            SetSkill(SkillName.Poisoning, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 85.2, 115.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 70.1, 90.0);

            Fame = 12500;
            Karma = -12500;

            VirtualArmor = 70;

            PackItem(new Nightshade(4));
            PackItem(new LesserPoisonPotion());
        }

        public override string CorpseName => "a poison elementals corpse";
        public override string DefaultName => "a poison elemental";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override Poison HitPoison => Poison.Lethal;
        public override double HitPoisonChance => 0.75;

        public override int TreasureMapLevel => 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls);
        }
    }
}
