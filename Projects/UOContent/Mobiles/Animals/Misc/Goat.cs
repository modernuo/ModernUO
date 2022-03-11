namespace Server.Mobiles
{
    public class Goat : BaseCreature
    {
        [Constructible]
        public Goat() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xD1;
            BaseSoundID = 0x99;

            SetStr(19);
            SetDex(15);
            SetInt(5);

            SetHits(12);
            SetMana(0);

            SetDamage(3, 4);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 15);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;
        }

        public Goat(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a goat corpse";
        public override string DefaultName => "a goat";

        public override int Meat => 2;
        public override int Hides => 8;
        public override FoodType FavoriteFood => FoodType.GrainsAndHay | FoodType.FruitsAndVegies;

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
