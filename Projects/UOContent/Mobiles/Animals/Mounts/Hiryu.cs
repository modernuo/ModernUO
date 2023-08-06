using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Hiryu : BaseMount
    {
        public override string DefaultName => "a hiryu";

        [Constructible]
        public Hiryu() : base(243, 0x3E94, AIType.AI_Melee)
        {
            Hue = GetHue();

            SetStr(1201, 1410);
            SetDex(171, 270);
            SetInt(301, 325);

            SetHits(901, 1100);
            SetMana(60);

            SetDamage(20, 30);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 70);
            SetResistance(ResistanceType.Fire, 70, 90);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Anatomy, 75.1, 80.0);
            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 100.1, 110.0);
            SetSkill(SkillName.Wrestling, 100.1, 120.0);

            Fame = 18000;
            Karma = -18000;

            Tamable = true;
            ControlSlots = 4;
            MinTameSkill = 98.7;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(3));
            }
        }

        public override int StepsMax => 4480;
        public override string CorpseName => "a hiryu corpse";
        public override double WeaponAbilityChance => 0.07; /* 1 in 15 chance of using per landed hit */

        public override bool StatLossAfterTame => true;

        public override int TreasureMapLevel => 5;
        public override int Meat => 16;
        public override int Hides => 60;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override bool CanAngerOnTame => true;

        private static MonsterAbility[] _abilities = { MonsterAbilities.GraspingClaw };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        private static int GetHue()
        {
            return Utility.Random(1075) switch
            {
                1074 => 0x85C,                                        // Strong Green     0.09%
                1073 => 0x490,                                        // Strong Purple    0.09%
                >= 1071 => 0x030,                                        // Green            0.19%
                >= 1069 => 0x037,                                        // Strong Yellow    0.19%
                >= 1066 => 0x295,                                        // Light Green      0.28%
                >= 1063 => 0x123,                                        // Cyan             0.28%
                >= 1058 => 0x482,                                        // Ice Blue         0.47%
                >= 1050 => 0x487,                                        // Blue/Yellow      0.74%
                >= 1040 => CraftResources.GetHue(CraftResource.Gold),    // Gold             0.93%
                >= 1030 => CraftResources.GetHue(CraftResource.Agapite), // Agapite          0.93%
                >= 1020 => 0x495,                                        // Strong Cyan      0.93%
                >= 1010 => 0x48D,                                        // Light Blue       0.93%
                >= 1000 => 0x47F,                                        // Ice Green        0.93%
                _ => 0                                             // No Hue          93.02%
            } | 0x8000;
        }

        public override int GetAngerSound() => 0x4FE;

        public override int GetIdleSound() => 0x4FD;

        public override int GetAttackSound() => 0x4FC;

        public override int GetHurtSound() => 0x4FF;

        public override int GetDeathSound() => 0x4FB;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 3);
            AddLoot(LootPack.Gems, 4);
        }
    }
}
