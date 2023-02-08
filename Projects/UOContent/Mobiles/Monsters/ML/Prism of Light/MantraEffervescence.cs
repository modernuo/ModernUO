using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MantraEffervescence : BaseCreature
    {
        [Constructible]
        public MantraEffervescence()
            : base(AIType.AI_Mage)
        {
            Body = 0x111;
            BaseSoundID = 0x56E;

            SetStr(130, 150);
            SetDex(120, 130);
            SetInt(150, 230);

            SetHits(150, 250);

            SetDamage(21, 25);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Energy, 70);

            SetResistance(ResistanceType.Physical, 60, 65);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 100);

            SetSkill(SkillName.Wrestling, 80.0, 85.0);
            SetSkill(SkillName.Tactics, 80.0, 85.0);
            SetSkill(SkillName.MagicResist, 105.0, 115.0);
            SetSkill(SkillName.Magery, 90.0, 110.0);
            SetSkill(SkillName.EvalInt, 80.0, 90.0);
            SetSkill(SkillName.Meditation, 90.0, 100.0);

            Fame = 6500;
            Karma = -6500;
        }

        public override string CorpseName => "a mantra effervescence corpse";
        public override string DefaultName => "a mantra effervescence";

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
        }
    }
}
