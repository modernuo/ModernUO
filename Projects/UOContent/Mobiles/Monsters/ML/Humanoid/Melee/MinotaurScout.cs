using Server.Items;

namespace Server.Mobiles
{
    public class MinotaurScout : BaseCreature
    {
        [Constructible]
        public MinotaurScout() : base(AIType.AI_Melee) // NEED TO CHECK
        {
            Body = 281;

            SetStr(353, 375);
            SetDex(111, 130);
            SetInt(34, 50);

            SetHits(354, 383);

            SetDamage(11, 20);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            // SetSkill( SkillName.Meditation, Unknown );
            // SetSkill( SkillName.EvalInt, Unknown );
            // SetSkill( SkillName.Magery, Unknown );
            // SetSkill( SkillName.Poisoning, Unknown );
            SetSkill(SkillName.Anatomy, 0);
            SetSkill(SkillName.MagicResist, 60.6, 67.5);
            SetSkill(SkillName.Tactics, 86.9, 103.6);
            SetSkill(SkillName.Wrestling, 85.6, 104.5);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 28; // Don't know what it should be
        }

        public MinotaurScout(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a minotaur corpse";

        public override string DefaultName => "a minotaur scout";

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
