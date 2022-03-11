using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    public class Bogling : BaseCreature
    {
        [Constructible]
        public Bogling() : base(AIType.AI_Melee)
        {
            Body = 779;
            BaseSoundID = 422;

            SetStr(96, 120);
            SetDex(91, 115);
            SetInt(21, 45);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 75.1, 100.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 55.1, 75.0);

            Fame = 450;
            Karma = -450;

            VirtualArmor = 28;

            PackItem(new Log(4));
            PackItem(new Seed());
        }

        public Bogling(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a plant corpse";
        public override string DefaultName => "a bogling";

        public override int Hides => 6;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
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
