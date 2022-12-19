using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Squirrel : BaseCreature
    {
        [Constructible]
        public Squirrel() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0x116;

            SetStr(44, 50);
            SetDex(35);
            SetInt(5);

            SetHits(42, 50);

            SetDamage(1, 2);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 34);
            SetResistance(ResistanceType.Fire, 10, 14);
            SetResistance(ResistanceType.Cold, 30, 35);
            SetResistance(ResistanceType.Poison, 20, 25);
            SetResistance(ResistanceType.Energy, 20, 25);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 4.0);

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -21.3;
        }

        public override string CorpseName => "a squirrel corpse";
        public override string DefaultName => "a squirrell";

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies;
    }
}
