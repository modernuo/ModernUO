using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GazerLarva : BaseCreature
    {
        [Constructible]
        public GazerLarva() : base(AIType.AI_Melee)
        {
            Body = 778;
            BaseSoundID = 377;

            SetStr(76, 100);
            SetDex(51, 75);
            SetInt(56, 80);

            SetHits(36, 47);

            SetDamage(2, 9);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 25);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 70.0);
            SetSkill(SkillName.Wrestling, 70.0);

            Fame = 900;
            Karma = -900;

            VirtualArmor = 25;

            PackItem(new Nightshade(Utility.RandomMinMax(2, 3)));
        }

        public override string CorpseName => "a gazer larva corpse";
        public override string DefaultName => "a gazer larva";

        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
        }
    }
}
