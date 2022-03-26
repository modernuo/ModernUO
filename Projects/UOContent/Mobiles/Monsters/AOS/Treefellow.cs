using Server.Items;

namespace Server.Mobiles
{
    public class Treefellow : BaseCreature
    {
        [Constructible]
        public Treefellow() : base(AIType.AI_Melee, FightMode.Evil)
        {
            Body = 301;

            SetStr(196, 220);
            SetDex(31, 55);
            SetInt(66, 90);

            SetHits(118, 132);

            SetDamage(12, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 30, 35);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 40.1, 55.0);
            SetSkill(SkillName.Tactics, 65.1, 90.0);
            SetSkill(SkillName.Wrestling, 65.1, 85.0);

            Fame = 500;
            Karma = 1500;

            VirtualArmor = 24;
            PackItem(new Log(Utility.RandomMinMax(23, 34)));
        }

        public Treefellow(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a treefellow corpse";

        public override string DefaultName => "a treefellow";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool BleedImmune => true;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        public override int GetIdleSound() => 443;

        public override int GetDeathSound() => 31;

        public override int GetAttackSound() => 672;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
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

            if (BaseSoundID == 442)
            {
                BaseSoundID = -1;
            }
        }
    }
}
