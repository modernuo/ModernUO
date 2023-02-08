using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Direwolf")]
    [SerializationGenerator(0, false)]
    public partial class DireWolf : BaseCreature
    {
        [Constructible]
        public DireWolf() : base(AIType.AI_Melee)
        {
            Body = 23;
            BaseSoundID = 0xE5;

            SetStr(96, 120);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(58, 72);
            SetMana(0);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 5, 10);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 57.6, 75.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 22;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 83.1;
        }

        public override string CorpseName => "a dire wolf corpse";
        public override string DefaultName => "a dire wolf";

        public override int Meat => 1;
        public override int Hides => 7;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Canine;
    }
}
