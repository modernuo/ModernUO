using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FrostSpider : BaseCreature
    {
        [Constructible]
        public FrostSpider() : base(AIType.AI_Melee)
        {
            Body = 20;
            BaseSoundID = 0x388;

            SetStr(76, 100);
            SetDex(126, 145);
            SetInt(36, 60);

            SetHits(46, 60);
            SetMana(0);

            SetDamage(6, 16);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 80);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 35.1, 50.0);
            SetSkill(SkillName.Wrestling, 50.1, 65.0);

            Fame = 775;
            Karma = -775;

            VirtualArmor = 28;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 74.7;

            PackItem(new SpidersSilk(7));
        }

        public override string CorpseName => "a frost spider corpse";
        public override string DefaultName => "a frost spider";

        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Arachnid;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Poor);
        }
    }
}
