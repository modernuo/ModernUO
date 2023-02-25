using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class RuneBeetle : BaseCreature
    {
        [Constructible]
        public RuneBeetle() : base(AIType.AI_Mage)
        {
            Body = 244;

            SetStr(401, 460);
            SetDex(121, 170);
            SetInt(376, 450);

            SetHits(301, 360);

            SetDamage(15, 22);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Poison, 10);
            SetDamageType(ResistanceType.Energy, 70);

            SetResistance(ResistanceType.Physical, 40, 65);
            SetResistance(ResistanceType.Fire, 35, 50);
            SetResistance(ResistanceType.Cold, 35, 50);
            SetResistance(ResistanceType.Poison, 75, 95);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.EvalInt, 100.1, 125.0);
            SetSkill(SkillName.Magery, 100.1, 110.0);
            SetSkill(SkillName.Poisoning, 120.1, 140.0);
            SetSkill(SkillName.MagicResist, 95.1, 110.0);
            SetSkill(SkillName.Tactics, 78.1, 93.0);
            SetSkill(SkillName.Wrestling, 70.1, 77.5);

            Fame = 15000;
            Karma = -15000;

            if (Utility.RandomDouble() < .25)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            PackItem(
                Utility.Random(10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    5 => new RibCage(),
                    _ => new BonePile() // 6-9
                }
            );

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 93.9;
        }

        public override string CorpseName => "a rune beetle corpse";
        public override string DefaultName => "a rune beetle";

        public override Poison PoisonImmune => Poison.Greater;
        public override Poison HitPoison => Poison.Greater;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
        public override bool CanAngerOnTame => true;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

        public override int GetAngerSound() => 0x4E8;

        public override int GetIdleSound() => 0x4E7;

        public override int GetAttackSound() => 0x4E6;

        public override int GetHurtSound() => 0x4E9;

        public override int GetDeathSound() => 0x4E5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.MedScrolls, 1);
        }

        private static MonsterAbility[] _abilities = { MonsterAbilities.RuneCorruption };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;
    }
}

