using Server.Items;

namespace Server.Mobiles
{
    public class MinotaurCaptain : BaseCreature
    {
        [Constructible]
        public MinotaurCaptain() : base(AIType.AI_Melee) // NEED TO CHECK
        {
            Body = 280;

            SetStr(401, 425);
            SetDex(91, 110);
            SetInt(31, 50);

            SetHits(401, 440);

            SetDamage(11, 20);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 65, 75);
            SetResistance(ResistanceType.Fire, 35, 45);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Meditation, 0);
            SetSkill(SkillName.EvalInt, 0);
            SetSkill(SkillName.Magery, 0);
            SetSkill(SkillName.Poisoning, 0);
            SetSkill(SkillName.Anatomy, 0, 6.3);
            SetSkill(SkillName.MagicResist, 66.1, 73.6);
            SetSkill(SkillName.Tactics, 93.0, 109.9);
            SetSkill(SkillName.Wrestling, 92.6, 107.2);

            Fame = 7000;
            Karma = -7000;

            VirtualArmor = 28; // Don't know what it should be
        }

        public MinotaurCaptain(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a minotaur corpse";

        public override string DefaultName => "a minotaur captain";

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich); // Need to verify
        }

        // Using Tormented Minotaur sounds - Need to veryfy
        public override int GetAngerSound() => 0x597;

        public override int GetIdleSound() => 0x596;

        public override int GetAttackSound() => 0x599;

        public override int GetHurtSound() => 0x59a;

        public override int GetDeathSound() => 0x59c;

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
