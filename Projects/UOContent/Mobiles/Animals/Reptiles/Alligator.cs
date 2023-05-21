using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Alligator : BaseCreature
    {
        [Constructible]
        public Alligator() : base(AIType.AI_Melee)
        {
            Body = 0xCA;
            BaseSoundID = 660;

            SetStr(76, 100);
            SetDex(6, 25);
            SetInt(11, 20);

            SetHits(46, 60);
            SetStam(46, 65);
            SetMana(0);

            SetDamage(5, 15);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Poison, 5, 10);

            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 40.1, 60.0);
            SetSkill(SkillName.Wrestling, 40.1, 60.0);

            Fame = 600;
            Karma = -600;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 47.1;
        }

        public override string CorpseName => "an alligator corpse";
        public override string DefaultName => "an alligator";

        public override int Meat => 1;
        public override int Hides => 12;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
    }
}
