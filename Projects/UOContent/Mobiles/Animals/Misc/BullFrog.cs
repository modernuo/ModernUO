namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Bullfrog")]
    public class BullFrog : BaseCreature
    {
        [Constructible]
        public BullFrog() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 81;
            Hue = Utility.RandomList(0x5AC, 0x5A3, 0x59A, 0x591, 0x588, 0x57F);
            BaseSoundID = 0x266;

            SetStr(46, 70);
            SetDex(6, 25);
            SetInt(11, 20);

            SetHits(28, 42);
            SetMana(0);

            SetDamage(1, 2);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 40.1, 60.0);
            SetSkill(SkillName.Wrestling, 40.1, 60.0);

            Fame = 350;
            Karma = 0;

            VirtualArmor = 6;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 23.1;
        }

        public BullFrog(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a bull frog corpse";
        public override string DefaultName => "a bull frog";

        public override int Meat => 1;
        public override int Hides => 4;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.Meat;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
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
