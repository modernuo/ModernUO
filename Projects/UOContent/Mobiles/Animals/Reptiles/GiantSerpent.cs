using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Serpant")]
    [SerializationGenerator(0, false)]
    public partial class GiantSerpent : BaseCreature
    {
        [Constructible]
        public GiantSerpent() : base(AIType.AI_Melee)
        {
            Body = 0x15;
            Hue = Utility.RandomSnakeHue();
            BaseSoundID = 219;

            SetStr(186, 215);
            SetDex(56, 80);
            SetInt(66, 85);

            SetHits(112, 129);
            SetMana(0);

            SetDamage(7, 17);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Poison, 60);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 70, 90);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.Poisoning, 70.1, 100.0);
            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 65.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 32;

            PackItem(new Bone());
            // TODO: Body parts
        }

        public override string CorpseName => "a giant serpent corpse";
        public override string DefaultName => "a giant snake";

        public override Poison PoisonImmune => Poison.Greater;
        public override Poison HitPoison => Utility.RandomDouble() < 0.8 ? Poison.Greater : Poison.Deadly;

        public override bool DeathAdderCharmable => true;

        public override int Meat => 4;
        public override int Hides => 15;
        public override HideType HideType => HideType.Spined;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
