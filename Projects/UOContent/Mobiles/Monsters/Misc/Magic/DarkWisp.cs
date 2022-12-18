using ModernUO.Serialization;
using System;
using Server.Ethics;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class DarkWisp : BaseCreature
    {
        [Constructible]
        public DarkWisp() : base(AIType.AI_Mage, FightMode.Aggressor)
        {
            Body = 165;
            BaseSoundID = 466;

            SetStr(196, 225);
            SetDex(196, 225);
            SetInt(196, 225);

            SetHits(118, 135);

            SetDamage(17, 18);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 10, 30);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 40;

            AddItem(new LightSource());
        }

        public override string CorpseName => "a wisp corpse";
        public override InhumanSpeech SpeechType => InhumanSpeech.Wisp;

        public override Ethic EthicAllegiance => Ethic.Evil;

        public override TimeSpan ReacquireDelay => TimeSpan.FromSeconds(1.0);

        public override string DefaultName => "a wisp";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
        }
    }
}
