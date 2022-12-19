using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class EliteNinja : BaseCreature
    {
        [Constructible]
        public EliteNinja() : base(AIType.AI_Melee)
        {
            SpeechHue = Utility.RandomDyedHue();
            Hue = Race.Human.RandomSkinHue();
            Female = Utility.RandomBool();

            Body = Female ? 0x191 : 0x190;

            SetHits(251, 350);

            SetStr(126, 225);
            SetDex(175, 275);
            SetInt(151, 165);

            SetDamage(12, 20);

            SetDamageType(ResistanceType.Physical, 65);
            SetDamageType(ResistanceType.Fire, 15);
            SetDamageType(ResistanceType.Poison, 15);
            SetDamageType(ResistanceType.Energy, 5);

            SetResistance(ResistanceType.Physical, 35, 65);
            SetResistance(ResistanceType.Fire, 40, 60);
            SetResistance(ResistanceType.Cold, 25, 45);
            SetResistance(ResistanceType.Poison, 40, 60);
            SetResistance(ResistanceType.Energy, 35, 55);

            SetSkill(SkillName.Anatomy, 105.0, 120.0);
            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 115.0, 130.0);
            SetSkill(SkillName.Wrestling, 95.0, 120.0);
            SetSkill(SkillName.Fencing, 95.0, 120.0);
            SetSkill(SkillName.Macing, 95.0, 120.0);
            SetSkill(SkillName.Swords, 95.0, 120.0);
            SetSkill(SkillName.Ninjitsu, 95.0, 120.0);

            Fame = 8500;
            Karma = -8500;

            /* TODO:
                Uses Smokebombs
                Hides
                Stealths
                Can use Ninjitsu Abilities
                Can change weapons during a fight
            */

            AddItem(new NinjaTabi());
            AddItem(new LeatherNinjaJacket());
            AddItem(new LeatherNinjaHood());
            AddItem(new LeatherNinjaPants());
            AddItem(new LeatherNinjaMitts());

            if (Utility.RandomDouble() < 0.33)
            {
                AddItem(new SmokeBomb());
            }

            AddItem(
                Utility.Random(8) switch
                {
                    0 => new Tessen(),
                    1 => new Wakizashi(),
                    2 => new Nunchaku(),
                    3 => new Daisho(),
                    4 => new Sai(),
                    5 => new Tekagi(),
                    6 => new Kama(),
                    _ => new Katana() // 7
                }
            );

            Utility.AssignRandomHair(this);
        }

        public override bool ClickTitle => false;
        public override string DefaultName => "an elite ninja";

        public override bool BardImmune => true;

        public override bool AlwaysMurderer => true;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);
            c.DropItem(new BookOfNinjitsu());
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
