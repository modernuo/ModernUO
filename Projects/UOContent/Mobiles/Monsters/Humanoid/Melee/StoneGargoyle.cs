using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class StoneGargoyle : BaseCreature
    {
        [Constructible]
        public StoneGargoyle() : base(AIType.AI_Melee)
        {
            Body = 67;
            BaseSoundID = 0x174;

            SetStr(246, 275);
            SetDex(76, 95);
            SetInt(81, 105);

            SetHits(148, 165);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 50;

            PackItem(new IronIngot(12));

            if (Utility.RandomDouble() < 0.05)
            {
                PackItem(new GargoylesPickaxe());
            }
        }

        public override string CorpseName => "a gargoyle corpse";
        public override string DefaultName => "a stone gargoyle";

        public override int TreasureMapLevel => 2;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.Gems, 1);
            AddLoot(LootPack.Potions);
        }
    }
}
