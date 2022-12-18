using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.OphidianAvenger")]
    [SerializationGenerator(0, false)]
    public partial class OphidianKnight : BaseCreature
    {
        private static readonly string[] m_Names =
        {
            "an ophidian knight-errant",
            "an ophidian avenger"
        };

        [Constructible]
        public OphidianKnight() : base(AIType.AI_Melee)
        {
            Name = m_Names.RandomElement();
            Body = 86;
            BaseSoundID = 634;

            SetStr(417, 595);
            SetDex(166, 175);
            SetInt(46, 70);

            SetHits(266, 342);
            SetMana(0);

            SetDamage(16, 19);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 90, 100);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 40;

            PackItem(new LesserPoisonPotion());
        }

        public override string CorpseName => "an ophidian corpse";

        public override int Meat => 2;

        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;
        public override int TreasureMapLevel => 3;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 2);
        }
    }
}
