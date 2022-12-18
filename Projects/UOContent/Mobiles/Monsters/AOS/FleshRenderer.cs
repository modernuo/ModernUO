using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FleshRenderer : BaseCreature
    {
        [Constructible]
        public FleshRenderer() : base(AIType.AI_Melee)
        {
            Body = 315;

            SetStr(401, 460);
            SetDex(201, 210);
            SetInt(221, 260);

            SetHits(4500);

            SetDamage(16, 20);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Poison, 20);

            SetResistance(ResistanceType.Physical, 80, 90);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 70, 80);

            SetSkill(SkillName.DetectHidden, 80.0);
            SetSkill(SkillName.MagicResist, 155.1, 160.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 23000;
            Karma = -23000;

            VirtualArmor = 24;
        }

        public override string CorpseName => "a fleshrenderer corpse";

        public override bool IgnoreYoungProtection => Core.ML;

        public override string DefaultName => "a fleshrenderer";

        public override bool AutoDispel => true;
        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool AreaPeaceImmune => Core.SE;
        public override Poison PoisonImmune => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() =>
            Utility.RandomBool() ? WeaponAbility.Dismount : WeaponAbility.ParalyzingBlow;

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

        public override int GetAttackSound() => 0x34C;

        public override int GetHurtSound() => 0x354;

        public override int GetAngerSound() => 0x34C;

        public override int GetIdleSound() => 0x34C;

        public override int GetDeathSound() => 0x354;
    }
}
