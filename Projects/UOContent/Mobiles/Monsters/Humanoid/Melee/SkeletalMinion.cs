using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SkeletalMinion : BaseCreature
    {
        [Constructible]
        public SkeletalMinion() : base(AIType.AI_Melee)
        {
            Body = Utility.RandomList(50, 56);
            BaseSoundID = 0x48D;

            SetStr(56, 80);
            SetDex(56, 75);
            SetInt(16, 40);

            SetHits(34, 48);

            SetDamage(3, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 25, 40);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 5, 15);

            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 450;
            Karma = -450;

            VirtualArmor = 16;
        }

        public override string CorpseName => "a skeletal minion corpse";
        public override string DefaultName => "a skeletal minion";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lesser;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;
        
        public override PackInstinct PackInstinct => PackInstinct.Minion;

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (target is SkeletalMinion || target is LichLord)
            {
                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }
    }
}
