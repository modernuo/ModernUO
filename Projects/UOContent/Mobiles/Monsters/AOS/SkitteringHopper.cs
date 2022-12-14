using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SkitteringHopper : BaseCreature
    {
        [Constructible]
        public SkitteringHopper() : base(AIType.AI_Melee, FightMode.Aggressor)
        {
            Body = 302;
            BaseSoundID = 959;

            SetStr(41, 65);
            SetDex(91, 115);
            SetInt(26, 50);

            SetHits(31, 45);

            SetDamage(3, 5);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 30.1, 45.0);
            SetSkill(SkillName.Tactics, 45.1, 70.0);
            SetSkill(SkillName.Wrestling, 40.1, 60.0);

            Fame = 300;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -12.9;

            VirtualArmor = 12;
        }

        public override string CorpseName => "a skittering hopper corpse";
        public override string DefaultName => "a skittering hopper";

        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
