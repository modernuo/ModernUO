using Server.Items;

namespace Server.Mobiles
{
    public class Reaper : BaseCreature
    {
        [Constructible]
        public Reaper() : base(AIType.AI_Mage)
        {
            Body = 47;
            BaseSoundID = 442;

            SetStr(66, 215);
            SetDex(66, 75);
            SetInt(101, 250);

            SetHits(40, 129);
            SetStam(0);

            SetDamage(9, 11);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Poison, 20);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 100.1, 125.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 50.1, 60.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 40;

            PackItem(new Log(10));
            PackItem(new MandrakeRoot(5));
        }

        public Reaper(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a reapers corpse";
        public override string DefaultName => "a reaper";

        public override Poison PoisonImmune => Poison.Greater;
        public override int TreasureMapLevel => 2;
        public override bool DisallowAllMoves => true;

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
        }
    }
}
