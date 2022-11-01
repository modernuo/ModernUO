namespace Server.Mobiles
{
    public abstract class BaseWarHorse : BaseMount
    {
        public override string DefaultName => "a war horse";

        public BaseWarHorse(
            int bodyID, int itemID, AIType aiType = AIType.AI_Melee, FightMode fightMode = FightMode.Aggressor,
            int rangePerception = 10, int rangeFight = 1
        ) : base(
            bodyID,
            itemID,
            aiType,
            fightMode,
            rangePerception,
            rangeFight
        )
        {
            BaseSoundID = 0xA8;

            InitStats(Utility.Random(300, 100), 125, 60);

            SetStr(400);
            SetDex(125);
            SetInt(51, 55);

            SetHits(240);
            SetMana(0);

            SetDamage(5, 8);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 300;
            Karma = 300;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public BaseWarHorse(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a war horse corpse";

        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
