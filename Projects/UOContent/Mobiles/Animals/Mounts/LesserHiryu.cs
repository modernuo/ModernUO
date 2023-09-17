using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class LesserHiryu : BaseMount
    {
        public override string DefaultName => "a lesser hiryu";

        [Constructible]
        public LesserHiryu() : base(243, 0x3E94, AIType.AI_Melee)
        {
            Hue = GetHue();

            SetStr(301, 410);
            SetDex(171, 270);
            SetInt(301, 325);

            SetHits(401, 600);
            SetMana(60);

            SetDamage(18, 23);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 70);
            SetResistance(ResistanceType.Fire, 60, 80);
            SetResistance(ResistanceType.Cold, 5, 15);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Anatomy, 75.1, 80.0);
            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 100.1, 110.0);
            SetSkill(SkillName.Wrestling, 100.1, 120.0);

            Fame = 10000;
            Karma = -10000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 98.7;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }
        }

        public override int StepsMax => 4480;
        public override string CorpseName => "a hiryu corpse";
        public override double WeaponAbilityChance => 0.07; /* 1 in 15 chance of using; 1 in 5 chance of success */

        public override bool StatLossAfterTame => true;

        public override int TreasureMapLevel => 3;
        public override int Meat => 16;
        public override int Hides => 60;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override bool CanAngerOnTame => true;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        private static MonsterAbility[] _abilities = { MonsterAbilities.GraspingClaw };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        private static int GetHue()
        {
            return Utility.Random(525) switch
            {
                524 => 0x258,                                         // Midnight Blue    0.19%
                523 => CraftResources.GetHue(CraftResource.Valorite), // Valorite         0.19%
                >= 520 => 0x7D4,                                         // Dark Green       0.57%
                >= 510 => 0x163,                                         // Green            1.90%
                >= 500 => 0x295,                                         // Green            1.90%
                _ => 0                                              // No Hue          95.24%
            } | 0x8000;
        }

        public override bool OverrideBondingReqs() => ControlMaster.Skills.Bushido.Base >= 90.0;

        public override int GetAngerSound() => 0x4FE;

        public override int GetIdleSound() => 0x4FD;

        public override int GetAttackSound() => 0x4FC;

        public override int GetHurtSound() => 0x4FF;

        public override int GetDeathSound() => 0x4FB;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 4);
        }

        public override double GetControlChance(Mobile m, bool useBaseSkill = false)
        {
            var tamingChance = base.GetControlChance(m, useBaseSkill);

            if (tamingChance < 0.05)
            {
                return tamingChance;
            }

            var skill = useBaseSkill ? m.Skills.Bushido.Base : m.Skills.Bushido.Value;

            if (skill < 90.0)
            {
                return tamingChance;
            }

            var bushidoChance = (skill - 30.0) / 100;

            if (m.Skills.Bushido.Base >= 120)
            {
                bushidoChance += 0.05;
            }

            return bushidoChance > tamingChance ? bushidoChance : tamingChance;
        }
    }
}
