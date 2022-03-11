using Server.Items;

namespace Server.Mobiles
{
    public class ArcaneDaemon : BaseCreature
    {
        [Constructible]
        public ArcaneDaemon() : base(AIType.AI_Mage)
        {
            Body = 0x310;
            BaseSoundID = 0x47D;

            SetStr(131, 150);
            SetDex(126, 145);
            SetInt(301, 350);

            SetHits(101, 115);

            SetDamage(12, 16);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Fire, 20);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 70, 80);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 85.1, 95.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);
            SetSkill(SkillName.Magery, 80.1, 90.0);
            SetSkill(SkillName.EvalInt, 70.1, 80.0);
            SetSkill(SkillName.Meditation, 70.1, 80.0);

            Fame = 7000;
            Karma = -10000;

            VirtualArmor = 55;
        }

        public ArcaneDaemon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an arcane daemon corpse";

        public override string DefaultName => "an arcane daemon";

        public override Poison PoisonImmune => Poison.Deadly;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ConcussionBlow;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
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
