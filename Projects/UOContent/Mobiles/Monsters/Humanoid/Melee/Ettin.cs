using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Ettin : BaseCreature
    {
        [Constructible]
        public Ettin() : base(AIType.AI_Melee)
        {
            Body = 18;
            BaseSoundID = 367;

            SetStr(136, 165);
            SetDex(56, 75);
            SetInt(31, 55);

            SetHits(82, 99);

            SetDamage(7, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 40.1, 55.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 50.1, 60.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 38;
        }

        public override string CorpseName => "an ettins corpse";
        public override string DefaultName => "an ettin";

        public override bool CanRummageCorpses => true;
        public override int TreasureMapLevel => 1;
        public override int Meat => 4;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Potions);
        }
    }
}
