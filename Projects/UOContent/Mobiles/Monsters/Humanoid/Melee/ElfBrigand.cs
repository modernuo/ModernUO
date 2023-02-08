using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    // TODO: Needs some Spellweaving abilities
    public partial class ElfBrigand : BaseCreature
    {
        [Constructible]
        public ElfBrigand() : base(AIType.AI_Melee)
        {
            SpeechHue = Utility.RandomDyedHue();
            Title = "the brigand";
            Race = Race.Elf;
            Hue = Race.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x25E;
                Name = NameList.RandomName("female elf brigand");

                switch (Utility.Random(2))
                {
                    case 0:
                        AddItem(new Skirt(Utility.RandomNondyedHue()));
                        break;
                    case 1:
                        AddItem(new Kilt(Utility.RandomNondyedHue()));
                        break;
                }
            }
            else
            {
                Body = 0x25D;
                Name = NameList.RandomName("male elf brigand");
                AddItem(new ShortPants(Utility.RandomNondyedHue()));
            }

            SetStr(86, 100);
            SetDex(81, 95);
            SetInt(61, 75);

            SetDamage(10, 23);

            SetSkill(SkillName.Fencing, 66.0, 97.5);
            SetSkill(SkillName.Macing, 65.0, 87.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);
            SetSkill(SkillName.Swords, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 15.0, 37.5);

            Fame = 1000;
            Karma = -1000;

            AddItem(
                Utility.Random(4) switch
                {
                    0 => new Boots(),
                    1 => new ThighBoots(),
                    2 => new Sandals(),
                    _ => new Shoes() // 3
                }
            );

            AddItem(new Shirt(Utility.RandomNondyedHue()));

            AddItem(
                Utility.Random(7) switch
                {
                    0 => new Longsword(),
                    1 => new Cutlass(),
                    2 => new Broadsword(),
                    3 => new Axe(),
                    4 => new Club(),
                    5 => new Dagger(),
                    _ => new Spear() // 6
                }
            );

            Utility.AssignRandomHair(this);
        }

        public override bool ClickTitle => false;

        public override bool AlwaysMurderer => true;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Utility.RandomDouble() < 0.9)
            {
                c.DropItem(new SeveredElfEars());
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
