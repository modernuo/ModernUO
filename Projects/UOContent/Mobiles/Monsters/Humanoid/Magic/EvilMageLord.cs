using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EvilMageLord : BaseCreature
    {
        [Constructible]
        public EvilMageLord() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("evil mage lord");
            Body = Core.UOR ? Utility.Random(125, 2) : 0x190;
            Hue = Core.UOR ? 0 : Race.Human.RandomSkinHue();

            SetStr(81, 105);
            SetDex(191, 215);
            SetInt(126, 150);

            SetHits(49, 63);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 80.2, 100.0);
            SetSkill(SkillName.Magery, 95.1, 100.0);
            SetSkill(SkillName.Meditation, 27.5, 50.0);
            SetSkill(SkillName.MagicResist, 77.5, 100.0);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 20.3, 80.0);

            Fame = 10500;
            Karma = -10500;

            VirtualArmor = 16;
            PackReg(23);

            PackItem(new Robe(Utility.RandomMetalHue()));
            PackItem(new WizardsHat(Utility.RandomMetalHue()));

            if (Utility.RandomBool())
            {
                PackItem(new Shoes());
            }
            else
            {
                PackItem(new Sandals());
            }
        }

        public override string CorpseName => "an evil mage lord corpse";

        public override bool CanRummageCorpses => true;
        public override bool AlwaysMurderer => true;
        public override int Meat => 1;
        public override int TreasureMapLevel => Core.AOS ? 2 : 0;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.MedScrolls, 2);
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Core.UOR)
            {
                if (Body == 0x190)
                {
                    Body = Utility.Random(125, 2);
                }

                if (Hue != 0)
                {
                    Hue = 0;
                }
            }
            else if (Body.BodyID is 125 or 126)
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
