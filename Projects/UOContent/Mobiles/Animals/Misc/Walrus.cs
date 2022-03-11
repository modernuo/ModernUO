namespace Server.Mobiles
{
    public class Walrus : BaseCreature
    {
        [Constructible]
        public Walrus() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xDD;
            BaseSoundID = 0xE0;

            SetStr(21, 29);
            SetDex(46, 55);
            SetInt(16, 20);

            SetHits(14, 17);
            SetMana(0);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 20, 25);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.2, 29.0);
            SetSkill(SkillName.Wrestling, 19.2, 29.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 18;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 35.1;
        }

        public Walrus(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a walrus corpse";
        public override string DefaultName => "a walrus";

        public override int Meat => 1;
        public override int Hides => 12;
        public override FoodType FavoriteFood => FoodType.Fish;

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
