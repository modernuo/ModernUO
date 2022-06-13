namespace Server.Mobiles
{
    public class Gorilla : BaseCreature
    {
        [Constructible]
        public Gorilla() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0x1D;
            BaseSoundID = 0x9E;

            SetStr(53, 95);
            SetDex(36, 55);
            SetInt(36, 60);

            SetHits(38, 51);
            SetMana(0);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 15);

            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 43.3, 58.0);
            SetSkill(SkillName.Wrestling, 43.3, 58.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 20;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -18.9;
        }

        public Gorilla(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a gorilla corpse";
        public override string DefaultName => "a gorilla";

        public override int Meat => 1;
        public override int Hides => 6;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

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
