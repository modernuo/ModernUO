namespace Server.Mobiles
{
    public class Eagle : BaseCreature
    {
        [Constructible]
        public Eagle() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 5;
            BaseSoundID = 0x2EE;

            SetStr(31, 47);
            SetDex(36, 60);
            SetInt(8, 20);

            SetHits(20, 27);
            SetMana(0);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 20, 25);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 15.3, 30.0);
            SetSkill(SkillName.Tactics, 18.1, 37.0);
            SetSkill(SkillName.Wrestling, 20.1, 30.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 22;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 17.1;
        }

        public Eagle(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a bird corpse";
        public override string DefaultName => "an eagle";

        public override int Meat => 1;
        public override MeatType MeatType => MeatType.Bird;
        public override int Feathers => 36;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
        public override bool CanFly => true;

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
