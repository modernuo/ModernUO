using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Protector : BaseCreature
    {
        [Constructible]
        public Protector()
            : base(AIType.AI_Melee)
        {
            Body = 401;
            Female = true;
            Hue = Race.Human.RandomSkinHue();
            HairItemID = Race.Human.RandomHair(this);
            HairHue = Race.Human.RandomHairHue();

            Title = "the mystic llamaherder";

            SetStr(700, 800);
            SetDex(100, 150);
            SetInt(50, 75);

            SetHits(350, 450);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 35, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Wrestling, 70.0, 100.0);
            SetSkill(SkillName.Tactics, 80.0, 100.0);
            SetSkill(SkillName.MagicResist, 50.0, 70.0);
            SetSkill(SkillName.Anatomy, 70.0, 100.0);

            Fame = 10000;
            Karma = -10000;

            Item boots = new ThighBoots();
            boots.Movable = false;
            boots.Hue = Utility.Random(2);

            var shroud = new Item(0x204E);
            shroud.Layer = Layer.OuterTorso;
            shroud.Movable = false;
            shroud.Hue = Utility.Random(2);

            AddItem(boots);
            AddItem(shroud);
        }

        public override string CorpseName => "a human corpse";
        public override string DefaultName => "a Protector";

        public override bool AlwaysMurderer => true;
        public override bool PropertyTitle => false;
        public override bool ShowFameTitle => false;

        public override void GenerateLoot(bool spawning)
        {
            if (spawning)
            {
                return; // No loot/backpack on spawn
            }

            base.GenerateLoot(true);
            base.GenerateLoot(false);
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.4)
            c.DropItem( new ProtectorsEssence() );
        }
        */
    }
}
