namespace Server.Mobiles
{
    public class Panther : BaseCreature
    {
        [Constructible]
        public Panther() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xD6;
            Hue = 0x901;
            BaseSoundID = 0x462;

            SetStr(61, 85);
            SetDex(86, 105);
            SetInt(26, 50);

            SetHits(37, 51);
            SetMana(0);

            SetDamage(4, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 5, 10);

            SetSkill(SkillName.MagicResist, 15.1, 30.0);
            SetSkill(SkillName.Tactics, 50.1, 65.0);
            SetSkill(SkillName.Wrestling, 50.1, 65.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 53.1;
        }

        public Panther(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a panther corpse";
        public override string DefaultName => "a panther";

        public override int Meat => 1;
        public override int Hides => 10;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
        public override PackInstinct PackInstinct => PackInstinct.Feline;

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
