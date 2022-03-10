using Server.Items;

namespace Server.Mobiles
{
    public class RevenantLion : BaseCreature
    {
        [Constructible]
        public RevenantLion() : base(AIType.AI_Mage)
        {
            Body = 251;

            SetStr(276, 325);
            SetDex(156, 175);
            SetInt(76, 105);

            SetHits(251, 280);

            SetDamage(18, 24);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Cold, 30);
            SetDamageType(ResistanceType.Poison, 10);
            SetDamageType(ResistanceType.Energy, 30);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 55, 65);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 80.1, 90.0);
            SetSkill(SkillName.Magery, 80.1, 90.0);
            SetSkill(SkillName.Poisoning, 120.1, 130.0);
            SetSkill(SkillName.MagicResist, 70.1, 90.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 80.1, 88.0);

            Fame = 4000;
            Karma = -4000;
            PackNecroReg(6, 8);

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
        }

        public RevenantLion(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a revenant lion corpse";
        public override string DefaultName => "a Revenant Lion";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Greater;
        public override Poison HitPoison => Poison.Greater;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

        public override int GetAngerSound() => 0x518;

        public override int GetIdleSound() => 0x517;

        public override int GetAttackSound() => 0x516;

        public override int GetHurtSound() => 0x519;

        public override int GetDeathSound() => 0x515;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 2);
            AddLoot(LootPack.MedScrolls, 2);

            // TODO: Bone Pile
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
