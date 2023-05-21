using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Corpser : BaseCreature
    {
        [Constructible]
        public Corpser() : base(AIType.AI_Melee)
        {
            Body = 8;
            BaseSoundID = 684;

            SetStr(156, 180);
            SetDex(26, 45);
            SetInt(26, 40);

            SetHits(94, 108);
            SetMana(0);

            SetDamage(10, 23);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Poison, 40);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 20, 30);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 18;

            if (Utility.RandomDouble() < 0.25)
            {
                PackItem(new Board(10));
            }
            else
            {
                PackItem(new Log(10));
            }

            PackItem(new MandrakeRoot(3));
        }

        public override string CorpseName => "a corpser corpse";
        public override string DefaultName => "a corpser";

        public override Poison PoisonImmune => Poison.Lesser;
        public override bool DisallowAllMoves => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
