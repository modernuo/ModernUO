namespace Server.Mobiles
{
    public class Dog : BaseCreature
    {
        [Constructible]
        public Dog() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xD9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x85;

            SetStr(27, 37);
            SetDex(28, 43);
            SetInt(29, 37);

            SetHits(17, 22);
            SetMana(0);

            SetDamage(4, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 10, 15);

            SetSkill(SkillName.MagicResist, 22.1, 47.0);
            SetSkill(SkillName.Tactics, 19.2, 31.0);
            SetSkill(SkillName.Wrestling, 19.2, 31.0);

            Fame = 0;
            Karma = 300;

            VirtualArmor = 12;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -15.3;
        }

        public Dog(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a dog corpse";
        public override string DefaultName => "a dog";

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Canine;

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
