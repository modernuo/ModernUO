using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Giantrat")]
    [SerializationGenerator(0, false)]
    public partial class GiantRat : BaseCreature
    {
        [Constructible]
        public GiantRat() : base(AIType.AI_Melee)
        {
            Body = 0xD7;
            BaseSoundID = 0x188;

            SetStr(32, 74);
            SetDex(46, 65);
            SetInt(16, 30);

            SetHits(26, 39);
            SetMana(0);

            SetDamage(4, 8);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Poison, 25, 35);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 300;
            Karma = -300;

            VirtualArmor = 18;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public override string CorpseName => "a giant rat corpse";
        public override string DefaultName => "a giant rat";

        public override int Meat => 1;
        public override int Hides => 6;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.Meat | FoodType.FruitsAndVegies | FoodType.Eggs;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
        }
    }
}
