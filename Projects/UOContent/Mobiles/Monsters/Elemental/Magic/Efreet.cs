using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Efreet : BaseCreature
    {
        [Constructible]
        public Efreet() : base(AIType.AI_Mage)
        {
            Body = 131;
            BaseSoundID = 768;

            SetStr(326, 355);
            SetDex(266, 285);
            SetInt(171, 195);

            SetHits(196, 213);

            SetDamage(11, 13);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Fire, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 60.1, 75.0);
            SetSkill(SkillName.Magery, 60.1, 75.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 56;
        }

        public override string CorpseName => "an efreet corpse";
        public override string DefaultName => "an efreet";

        public override int TreasureMapLevel => Core.AOS ? 4 : 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems);

            if (Utility.RandomDouble() < 0.02)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        PackItem(new DaemonArms());
                        break;
                    case 1:
                        PackItem(new DaemonChest());
                        break;
                    case 2:
                        PackItem(new DaemonGloves());
                        break;
                    case 3:
                        PackItem(new DaemonLegs());
                        break;
                    case 4:
                        PackItem(new DaemonHelm());
                        break;
                }
            }
        }
    }
}
