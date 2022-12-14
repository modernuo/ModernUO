using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Gibberling : BaseCreature
    {
        [Constructible]
        public Gibberling() : base(AIType.AI_Melee)
        {
            Body = 307;
            BaseSoundID = 422;

            SetStr(141, 165);
            SetDex(101, 125);
            SetInt(56, 80);

            SetHits(85, 99);

            SetDamage(12, 17);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Fire, 40);
            SetDamageType(ResistanceType.Energy, 60);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 45.1, 70.0);
            SetSkill(SkillName.Tactics, 67.6, 92.5);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 27;
        }

        public override string CorpseName => "a gibberling corpse";

        public override string DefaultName => "a gibberling";

        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
