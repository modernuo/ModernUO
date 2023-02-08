using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Cougar : BaseCreature
    {
        [Constructible]
        public Cougar() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 63;
            BaseSoundID = 0x73;

            SetStr(56, 80);
            SetDex(66, 85);
            SetInt(26, 50);

            SetHits(34, 48);
            SetMana(0);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 5, 10);

            SetSkill(SkillName.MagicResist, 15.1, 30.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 41.1;
        }

        public override string CorpseName => "a cougar corpse";
        public override string DefaultName => "a cougar";

        public override int Meat => 1;
        public override int Hides => 10;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Feline;
    }
}
