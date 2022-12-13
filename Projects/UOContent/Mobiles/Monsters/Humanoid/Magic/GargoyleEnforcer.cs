using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GargoyleEnforcer : BaseCreature
    {
        [Constructible]
        public GargoyleEnforcer() : base(AIType.AI_Mage)
        {
            Body = 0x2F2;
            BaseSoundID = 0x174;

            SetStr(760, 850);
            SetDex(102, 150);
            SetInt(152, 200);

            SetHits(482, 485);

            SetDamage(7, 14);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 120.1, 130.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Wrestling, 80.1, 90.0);
            SetSkill(SkillName.Swords, 80.1, 90.0);
            SetSkill(SkillName.Anatomy, 70.1, 80.0);
            SetSkill(SkillName.Magery, 80.1, 90.0);
            SetSkill(SkillName.EvalInt, 70.3, 100.0);
            SetSkill(SkillName.Meditation, 70.3, 100.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 50;

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new GargoylesPickaxe());
            }
        }

        public override string CorpseName => "a gargoyle corpse";

        public override string DefaultName => "a gargoyle enforcer";

        public override bool CanFly => true;

        public override int Meat => 1;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.WhirlwindAttack;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls);
        }
    }
}
