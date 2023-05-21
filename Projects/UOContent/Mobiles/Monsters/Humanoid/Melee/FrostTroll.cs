using Server.Items;
using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FrostTroll : BaseCreature
    {
        [Constructible]
        public FrostTroll() : base(AIType.AI_Melee)
        {
            Body = 55;
            BaseSoundID = 461;

            SetStr(227, 265);
            SetDex(66, 85);
            SetInt(46, 70);

            SetHits(140, 156);

            SetDamage(14, 20);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Cold, 25);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 50;

            PackItem(new DoubleAxe()); // TODO: Weapon??
        }

        public override string CorpseName => "a frost troll corpse";
        public override string DefaultName => "a frost troll";

        public override int Meat => 2;
        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems);
        }
    }
}
