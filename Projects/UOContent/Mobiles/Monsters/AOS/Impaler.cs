using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Impaler : BaseCreature
    {
        [Constructible]
        public Impaler() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("impaler");
            Body = 306;
            BaseSoundID = 0x2A7;

            SetStr(190);
            SetDex(45);
            SetInt(190);

            SetHits(5000);

            SetDamage(31, 35);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 90);
            SetResistance(ResistanceType.Fire, 60);
            SetResistance(ResistanceType.Cold, 75);
            SetResistance(ResistanceType.Poison, 60);
            SetResistance(ResistanceType.Energy, 100);

            SetSkill(SkillName.DetectHidden, 80.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Poisoning, 160.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 49;
        }

        public override string CorpseName => "an impaler corpse";

        public override bool IgnoreYoungProtection => Core.ML;

        public override bool AutoDispel => true;
        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool AreaPeaceImmune => Core.SE;
        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Utility.RandomDouble() < 0.8 ? Poison.Greater : Poison.Deadly;

        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() =>
            Utility.RandomBool() ? WeaponAbility.MortalStrike : WeaponAbility.BleedAttack;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
            {
                DemonKnight.DistributeArtifact(this);
            }
        }
    }
}
