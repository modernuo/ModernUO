using ModernUO.Serialization;

namespace Server.Mobiles
{
    public partial class ForestOstard : BaseMount
    {
        public override string DefaultName => "a forest ostard";

        [Constructible]
        public ForestOstard() : base(0xDB, 0x3EA5, AIType.AI_Animal, FightMode.Aggressor)
        {
            Hue = Utility.RandomSlimeHue() | 0x8000;

            BaseSoundID = 0x270;

            SetStr(94, 170);
            SetDex(56, 75);
            SetInt(6, 10);

            SetHits(71, 88);
            SetMana(0);

            SetDamage(8, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);

            SetSkill(SkillName.MagicResist, 27.1, 32.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
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
