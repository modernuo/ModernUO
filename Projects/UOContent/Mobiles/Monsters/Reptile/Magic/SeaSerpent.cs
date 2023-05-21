using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Seaserpant")]
    [SerializationGenerator(0, false)]
    public partial class SeaSerpent : BaseCreature
    {
        [Constructible]
        public SeaSerpent() : base(AIType.AI_Mage)
        {
            Body = 150;
            BaseSoundID = 447;

            Hue = Utility.Random(0x530, 9);

            SetStr(168, 225);
            SetDex(58, 85);
            SetInt(53, 95);

            SetHits(110, 127);

            SetDamage(7, 13);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 15, 20);

            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 30;
            CanSwim = true;
            CantWalk = true;

            if (Utility.RandomBool())
            {
                PackItem(new SulfurousAsh(4));
            }
            else
            {
                PackItem(new BlackPearl(4));
            }

            PackItem(new RawFishSteak());

            // PackItem( new SpecialFishingNet() );
        }

        public override string CorpseName => "a sea serpents corpse";
        public override string DefaultName => "a sea serpent";
        public override int TreasureMapLevel => 2;

        public override int Hides => 10;
        public override HideType HideType => HideType.Horned;
        public override int Scales => 8;
        public override ScaleType ScaleType => ScaleType.Blue;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
