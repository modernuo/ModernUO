using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EvilMage : BaseCreature
    {
        [Constructible]
        public EvilMage() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("evil mage");
            Title = "the evil mage";
            Body = Core.UOR ? 124 : 0x190;

            SetStr(81, 105);
            SetDex(91, 115);
            SetInt(96, 120);

            SetHits(49, 63);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.EvalInt, 75.1, 100.0);
            SetSkill(SkillName.Magery, 75.1, 100.0);
            SetSkill(SkillName.MagicResist, 75.0, 97.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 20.2, 60.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 16;
            PackReg(6);
            EquipItem(new Robe(Utility.RandomNeutralHue()));
            EquipItem(new Sandals());
        }

        public override string CorpseName => "an evil mage corpse";

        public override bool CanRummageCorpses => true;
        public override bool AlwaysMurderer => true;
        public override int Meat => 1;
        public override int TreasureMapLevel => Core.AOS ? 1 : 0;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.MedScrolls);
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Core.UOR)
            {
                if (Body == 0x190)
                {
                    Body = 124;
                }

                if (Hue != 0)
                {
                    Hue = 0;
                }
            }
            else if (Body == 124)
            {
                Body = 0x190;

                if (Hue == 0)
                {
                    Hue = Race.Human.RandomSkinHue();
                }
            }
        }
    }
}
