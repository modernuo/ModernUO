using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Bogle : BaseCreature
    {
        [Constructible]
        public Bogle() : base(AIType.AI_Mage)
        {
            Body = 153;
            BaseSoundID = 0x482;

            SetStr(76, 100);
            SetDex(76, 95);
            SetInt(36, 60);

            SetHits(46, 60);

            SetDamage(7, 11);

            SetSkill(SkillName.EvalInt, 55.1, 70.0);
            SetSkill(SkillName.Magery, 55.1, 70.0);
            SetSkill(SkillName.MagicResist, 55.1, 70.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 28;
            PackItem(Loot.RandomWeapon());
            PackItem(new Bone());
        }

        public override string CorpseName => "a ghostly corpse";
        public override string DefaultName => "a bogle";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }
    }
}
