using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SnowElemental : BaseCreature
    {
        [Constructible]
        public SnowElemental() : base(AIType.AI_Melee)
        {
            Body = 163;
            BaseSoundID = 263;

            SetStr(326, 355);
            SetDex(166, 185);
            SetInt(71, 95);

            SetHits(196, 213);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 80);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 60, 70);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.MagicResist, 50.1, 65.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 50;

            PackItem(new BlackPearl(3));
            Item ore = new IronOre(3);
            ore.ItemID = 0x19B8;
            PackItem(ore);
        }

        public override string CorpseName => "a snow elemental corpse";
        public override string DefaultName => "a snow elemental";

        public override bool BleedImmune => true;

        public override int TreasureMapLevel => Utility.RandomList(2, 3);

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
        }
    }
}
