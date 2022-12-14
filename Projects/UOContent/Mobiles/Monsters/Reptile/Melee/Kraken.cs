using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Kraken : BaseCreature
    {
        [Constructible]
        public Kraken() : base(AIType.AI_Melee)
        {
            Body = 77;
            BaseSoundID = 353;

            SetStr(756, 780);
            SetDex(226, 245);
            SetInt(26, 40);

            SetHits(454, 468);
            SetMana(0);

            SetDamage(19, 33);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Cold, 30);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 11000;
            Karma = -11000;

            VirtualArmor = 50;

            CanSwim = true;
            CantWalk = true;

            var rope = new Rope();
            rope.ItemID = 0x14F8;
            PackItem(rope);

            if (Utility.RandomDouble() < .05)
            {
                PackItem(new MessageInABottle());
            }

            PackItem(new SpecialFishingNet()); // Confirm?
        }

        public override string CorpseName => "a krakens corpse";
        public override string DefaultName => "a kraken";

        public override int TreasureMapLevel => 4;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
        }
    }
}
