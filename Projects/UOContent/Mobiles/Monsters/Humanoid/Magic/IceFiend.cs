namespace Server.Mobiles
{
    public class IceFiend : BaseCreature
    {
        [Constructible]
        public IceFiend() : base(AIType.AI_Mage)
        {
            Body = 43;
            BaseSoundID = 357;

            SetStr(376, 405);
            SetDex(176, 195);
            SetInt(201, 225);

            SetHits(226, 243);

            SetDamage(8, 19);

            SetSkill(SkillName.EvalInt, 80.1, 90.0);
            SetSkill(SkillName.Magery, 80.1, 90.0);
            SetSkill(SkillName.MagicResist, 75.1, 85.0);
            SetSkill(SkillName.Tactics, 80.1, 90.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 60, 70);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 30, 40);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 60;
        }

        public IceFiend(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ice fiend corpse";
        public override string DefaultName => "an ice fiend";

        public override int TreasureMapLevel => 4;
        public override int Meat => 1;
        public override bool CanFly => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.MedScrolls, 2);
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
