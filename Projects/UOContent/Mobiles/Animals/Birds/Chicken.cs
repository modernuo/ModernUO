using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Chicken : BaseCreature
    {
        [Constructible]
        public Chicken() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xD0;
            BaseSoundID = 0x6E;

            SetStr(5);
            SetDex(15);
            SetInt(5);

            SetHits(3);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 1, 5);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 2;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override string CorpseName => "a chicken corpse";
        public override string DefaultName => "a chicken";

        public override int Meat => 1;
        public override MeatType MeatType => MeatType.Bird;
        public override FoodType FavoriteFood => FoodType.GrainsAndHay;
        public override bool CanFly => true;

        public override int Feathers => 25;
    }
}
