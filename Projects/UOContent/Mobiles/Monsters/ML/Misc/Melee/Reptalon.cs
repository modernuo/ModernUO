using Server.Items;

namespace Server.Mobiles
{
    public class Reptalon : BaseMount
    {
        public override string DefaultName => "a reptalon";

        [Constructible]
        public Reptalon() : base(0x114, 0x3E90, AIType.AI_Melee)
        {
            BaseSoundID = 0x16A;

            SetStr(1001, 1025);
            SetDex(152, 164);
            SetInt(251, 289);

            SetHits(833, 931);

            SetDamage(21, 28);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Poison, 25);
            SetDamageType(ResistanceType.Energy, 75);

            SetResistance(ResistanceType.Physical, 53, 64);
            SetResistance(ResistanceType.Fire, 35, 45);
            SetResistance(ResistanceType.Cold, 36, 45);
            SetResistance(ResistanceType.Poison, 52, 63);
            SetResistance(ResistanceType.Energy, 71, 83);

            SetSkill(SkillName.Wrestling, 101.5, 118.2);
            SetSkill(SkillName.Tactics, 101.7, 108.2);
            SetSkill(SkillName.MagicResist, 76.4, 89.9);
            SetSkill(SkillName.Anatomy, 56.4, 59.7);

            Tamable = true;
            ControlSlots = 4;
            MinTameSkill = 101.1;
        }

        public Reptalon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a reptalon corpse";

        public override int TreasureMapLevel => 5;
        public override int Meat => 5;
        public override int Hides => 10;
        public override bool CanAngerOnTame => true;
        public override bool StatLossAfterTame => true;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override bool CanFly => true;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.AosUltraRich, 3);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
