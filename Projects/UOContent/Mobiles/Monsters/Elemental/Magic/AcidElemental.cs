using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.ToxicElemental")]
    [SerializationGenerator(0, false)]
    public partial class AcidElemental : BaseCreature
    {
        [Constructible]
        public AcidElemental() : base(AIType.AI_Mage)
        {
            Body = 0x9E;
            BaseSoundID = 278;

            SetStr(326, 355);
            SetDex(66, 85);
            SetInt(271, 295);

            SetHits(196, 213);

            SetDamage(9, 15);

            SetDamageType(ResistanceType.Physical, 25);
            SetDamageType(ResistanceType.Fire, 50);
            SetDamageType(ResistanceType.Energy, 25);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Anatomy, 30.3, 60.0);
            SetSkill(SkillName.EvalInt, 70.1, 85.0);
            SetSkill(SkillName.Magery, 70.1, 85.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 80.1, 90.0);
            SetSkill(SkillName.Wrestling, 70.1, 90.0);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 40;
        }

        public override string CorpseName => "an acid elemental corpse";
        public override string DefaultName => "an acid elemental";

        public override bool BleedImmune => true;
        public override Poison HitPoison => Poison.Lethal;
        public override double HitPoisonChance => 0.6;

        public override int TreasureMapLevel => Core.AOS ? 2 : 3;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
        }
    }
}
