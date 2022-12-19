using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Tangle : BogThing
    {
        [Constructible]
        public Tangle()
        {
            // TODO: Not a paragon? No ML arties?
            // It moves like a paragon on OSI...

            Hue = 0x21;

            SetStr(870, 940);
            SetDex(58, 74);
            SetInt(46, 58);

            SetHits(2468, 2733);
            SetMana(8, 12);

            SetDamage(15, 28);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Poison, 40);

            SetResistance(ResistanceType.Physical, 50, 57);
            SetResistance(ResistanceType.Fire, 40, 43);
            SetResistance(ResistanceType.Cold, 30, 35);
            SetResistance(ResistanceType.Poison, 61, 69);
            SetResistance(ResistanceType.Energy, 41, 45);

            SetSkill(SkillName.Wrestling, 77.8, 94.6);
            SetSkill(SkillName.Tactics, 90.6, 100.4);
            SetSkill(SkillName.MagicResist, 108.4, 114.0);

            // TODO: Fame/Karma?
        }

        public override string CorpseName => "a Tangle corpse";
        public override string DefaultName => "Tangle";

        public override Poison PoisonImmune => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Utility.RandomDouble() < 0.3)
            {
                c.DropItem(new TaintedSeeds());
            }
        }
    }
}
