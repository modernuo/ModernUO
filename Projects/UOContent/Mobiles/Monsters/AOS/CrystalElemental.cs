using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CrystalElemental : BaseCreature
    {
        [Constructible]
        public CrystalElemental() : base(AIType.AI_Mage)
        {
            Body = 300;
            BaseSoundID = 278;

            SetStr(136, 160);
            SetDex(51, 65);
            SetInt(86, 110);

            SetHits(150);

            SetDamage(10, 15);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 55, 70);

            SetSkill(SkillName.EvalInt, 70.1, 75.0);
            SetSkill(SkillName.Magery, 70.1, 75.0);
            SetSkill(SkillName.Meditation, 65.1, 75.0);
            SetSkill(SkillName.MagicResist, 80.1, 90.0);
            SetSkill(SkillName.Tactics, 75.1, 85.0);
            SetSkill(SkillName.Wrestling, 65.1, 75.0);

            Fame = 6500;
            Karma = -6500;

            VirtualArmor = 54;
        }

        public override string CorpseName => "a crystal elemental corpse";

        public override string DefaultName => "a crystal elemental";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
        }
    }
}
