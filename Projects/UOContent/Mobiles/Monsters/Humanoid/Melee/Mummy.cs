using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class Mummy : BaseCreature
    {
        [Constructible]
        public Mummy() : base(AIType.AI_Melee)
        {
            Body = 154;
            BaseSoundID = 471;

            SetStr(346, 370);
            SetDex(71, 90);
            SetInt(26, 40);

            SetHits(208, 222);

            SetDamage(13, 23);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Cold, 60);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 15.1, 40.0);
            SetSkill(SkillName.Tactics, 35.1, 50.0);
            SetSkill(SkillName.Wrestling, 35.1, 50.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 50;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(2));
            }

            PackItem(new Garlic(5));
            PackItem(new Bandage(10));
        }

        public Mummy(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a mummy corpse";
        public override string DefaultName => "a mummy";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lesser;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems);
            AddLoot(LootPack.Potions);
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
