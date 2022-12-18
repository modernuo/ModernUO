using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Timberwolf")]
    [SerializationGenerator(0, false)]
    public partial class TimberWolf : BaseCreature
    {
        [Constructible]
        public TimberWolf() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 225;
            BaseSoundID = 0xE5;

            SetStr(56, 80);
            SetDex(56, 75);
            SetInt(11, 25);

            SetHits(34, 48);
            SetMana(0);

            SetDamage(5, 9);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 27.6, 45.0);
            SetSkill(SkillName.Tactics, 30.1, 50.0);
            SetSkill(SkillName.Wrestling, 40.1, 60.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 23.1;
        }

        public override string CorpseName => "a timber wolf corpse";
        public override string DefaultName => "a timber wolf";

        public override int Meat => 1;
        public override int Hides => 5;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Canine;
    }
}
