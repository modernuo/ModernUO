using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Whitewolf")]
    [SerializationGenerator(0, false)]
    public partial class WhiteWolf : BaseCreature
    {
        [Constructible]
        public WhiteWolf() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = Utility.RandomList(34, 37);
            BaseSoundID = 0xE5;

            SetStr(56, 80);
            SetDex(56, 75);
            SetInt(31, 55);

            SetHits(34, 48);
            SetMana(0);

            SetDamage(3, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 20, 25);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 20.1, 35.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 65.1;
        }

        public override string CorpseName => "a white wolf corpse";
        public override string DefaultName => "a white wolf";

        public override int Meat => 1;
        public override int Hides => 6;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Canine;
    }
}
