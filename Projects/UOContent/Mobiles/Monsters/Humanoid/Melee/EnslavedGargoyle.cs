using Server.Items;
using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EnslavedGargoyle : BaseCreature
    {
        [Constructible]
        public EnslavedGargoyle() : base(AIType.AI_Melee)
        {
            Body = 0x2F1;
            BaseSoundID = 0x174;

            SetStr(302, 360);
            SetDex(76, 95);
            SetInt(81, 105);

            SetHits(186, 212);

            SetDamage(7, 14);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 50, 70);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 25, 30);
            SetResistance(ResistanceType.Energy, 25, 30);

            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 40.1, 80.0);

            Fame = 3500;
            Karma = 0;

            VirtualArmor = 35;

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new GargoylesPickaxe());
            }
        }

        public override string CorpseName => "an enslaved gargoyle corpse";
        public override string DefaultName => "an enslaved gargoyle";

        public override int Meat => 1;
        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.Gems);
        }
    }
}
