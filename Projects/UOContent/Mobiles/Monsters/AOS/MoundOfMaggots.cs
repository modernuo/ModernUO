using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MoundOfMaggots : BaseCreature
    {
        [Constructible]
        public MoundOfMaggots() : base(AIType.AI_Melee)
        {
            Body = 319;
            BaseSoundID = 898;

            SetStr(61, 70);
            SetDex(61, 70);
            SetInt(10);

            SetMana(0);

            SetDamage(3, 9);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 90);
            SetResistance(ResistanceType.Poison, 100);

            SetSkill(SkillName.Tactics, 50.0);
            SetSkill(SkillName.Wrestling, 50.1, 60.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 24;
        }

        public override string CorpseName => "a maggoty corpse";
        public override string DefaultName => "a mound of maggots";

        public override Poison PoisonImmune => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Gems);
        }
    }
}
