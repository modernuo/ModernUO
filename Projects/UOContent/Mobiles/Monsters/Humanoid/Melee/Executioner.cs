using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Executioner : BaseCreature
    {
        [Constructible]
        public Executioner() : base(AIType.AI_Melee)
        {
            SpeechHue = Utility.RandomDyedHue();
            Title = "the executioner";
            Hue = Race.Human.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                AddItem(new Skirt(Utility.RandomRedHue()));
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                AddItem(new ShortPants(Utility.RandomRedHue()));
            }

            SetStr(386, 400);
            SetDex(151, 165);
            SetInt(161, 175);

            SetDamage(8, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 25, 30);
            SetResistance(ResistanceType.Cold, 25, 30);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.Anatomy, 125.0);
            SetSkill(SkillName.Fencing, 46.0, 77.5);
            SetSkill(SkillName.Macing, 35.0, 57.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 83.5, 92.5);
            SetSkill(SkillName.Swords, 125.0);
            SetSkill(SkillName.Tactics, 125.0);
            SetSkill(SkillName.Lumberjacking, 125.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 40;

            AddItem(new ThighBoots(Utility.RandomRedHue()));
            AddItem(new Surcoat(Utility.RandomRedHue()));
            AddItem(new ExecutionersAxe());

            Utility.AssignRandomHair(this);
        }

        public override bool AlwaysMurderer => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Meager);
        }
    }
}
