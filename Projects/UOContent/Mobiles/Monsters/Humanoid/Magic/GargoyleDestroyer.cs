using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GargoyleDestroyer : BaseCreature
    {
        [Constructible]
        public GargoyleDestroyer() : base(AIType.AI_Mage)
        {
            Body = 0x2F3;
            BaseSoundID = 0x174;

            SetStr(760, 850);
            SetDex(102, 150);
            SetInt(152, 200);

            SetHits(482, 485);

            SetDamage(7, 14);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.Wrestling, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.4, 160.0);
            SetSkill(SkillName.Anatomy, 50.5, 100.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Macing, 90.1, 100.0);
            SetSkill(SkillName.Fencing, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 50;

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new GargoylesPickaxe());
            }
        }

        public override string CorpseName => "a gargoyle corpse";
        public override string DefaultName => "a gargoyle destroyer";

        public override bool BardImmune => !Core.AOS;
        public override int Meat => 1;
        public override bool CanFly => true;

        private static MonsterAbility[] _abilities = { MonsterAbilities.ThrowHatchetCounter };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
