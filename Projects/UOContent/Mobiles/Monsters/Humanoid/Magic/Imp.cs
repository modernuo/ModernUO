namespace Server.Mobiles
{
    public class Imp : BaseCreature
    {
        [Constructible]
        public Imp() : base(AIType.AI_Mage)
        {
            Body = 74;
            BaseSoundID = 422;

            SetStr(91, 115);
            SetDex(61, 80);
            SetInt(86, 105);

            SetHits(55, 70);

            SetDamage(10, 14);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Fire, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 20.1, 30.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 30.1, 50.0);
            SetSkill(SkillName.Tactics, 42.1, 50.0);
            SetSkill(SkillName.Wrestling, 40.1, 44.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 2;
            MinTameSkill = 83.1;
        }

        public Imp(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an imp corpse";
        public override string DefaultName => "an imp";

        public override int Meat => 1;
        public override int Hides => 6;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Daemon;
        public override bool CanFly => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
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
