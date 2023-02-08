using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class DesertOstard : BaseMount
    {
        public override string DefaultName => "a desert ostard";

        [Constructible]
        public DesertOstard() : base(0xD2, 0x3EA3, AIType.AI_Animal, FightMode.Aggressor)
        {
            BaseSoundID = 0x270;

            SetStr(94, 170);
            SetDex(56, 75);
            SetInt(6, 10);

            SetHits(71, 88);
            SetMana(0);

            SetDamage(5, 11);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 15);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 25.3, 40.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 450;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public override string CorpseName => "an ostard corpse";

        public override int Meat => 3;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
        public override PackInstinct PackInstinct => PackInstinct.Ostard;
    }
}
