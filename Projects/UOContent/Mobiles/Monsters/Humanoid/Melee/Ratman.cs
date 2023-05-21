using ModernUO.Serialization;
using Server.Misc;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Ratman : BaseCreature
    {
        [Constructible]
        public Ratman() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("ratman");
            Body = 42;
            BaseSoundID = 437;

            SetStr(96, 120);
            SetDex(81, 100);
            SetInt(36, 60);

            SetHits(58, 72);

            SetDamage(4, 5);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 35.1, 60.0);
            SetSkill(SkillName.Tactics, 50.1, 75.0);
            SetSkill(SkillName.Wrestling, 50.1, 75.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 28;
        }

        public override string CorpseName => "a ratman's corpse";
        public override InhumanSpeech SpeechType => InhumanSpeech.Ratman;

        public override bool CanRummageCorpses => true;
        public override int Hides => 8;
        public override HideType HideType => HideType.Spined;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            // TODO: weapon, misc
        }
    }
}
