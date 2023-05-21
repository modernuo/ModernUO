using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Lavalizard")]
    [SerializationGenerator(0, false)]
    public partial class LavaLizard : BaseCreature
    {
        [Constructible]
        public LavaLizard() : base(AIType.AI_Melee)
        {
            Body = 0xCE;
            Hue = Utility.RandomList(0x647, 0x650, 0x659, 0x662, 0x66B, 0x674);
            BaseSoundID = 0x5A;

            SetStr(126, 150);
            SetDex(56, 75);
            SetInt(11, 20);

            SetHits(76, 90);
            SetMana(0);

            SetDamage(6, 24);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 30, 45);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.MagicResist, 55.1, 70.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 80.7;

            PackItem(new SulfurousAsh(Utility.Random(4, 10)));
        }

        public override string CorpseName => "a lava lizard corpse";
        public override string DefaultName => "a lava lizard";
        public override int Hides => 12;
        public override HideType HideType => HideType.Spined;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
