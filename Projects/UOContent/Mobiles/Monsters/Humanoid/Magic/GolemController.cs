using Server.Items;

namespace Server.Mobiles
{
    public class GolemController : BaseCreature
    {
        [Constructible]
        public GolemController() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("golem controller");
            Title = "the controller";

            Body = 400;
            Hue = 0x455;

            AddArcane(new Robe());
            AddArcane(new ThighBoots());
            AddArcane(new LeatherGloves());
            AddArcane(new Cloak());

            SetStr(126, 150);
            SetDex(96, 120);
            SetInt(151, 175);

            SetHits(76, 90);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 5, 15);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.EvalInt, 95.1, 100.0);
            SetSkill(SkillName.Magery, 95.1, 100.0);
            SetSkill(SkillName.Meditation, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 102.5, 125.0);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 65.0, 87.5);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 16;

            if (Utility.RandomDouble() < 0.7)
            {
                PackItem(new ArcaneGem());
            }
        }

        public GolemController(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a golem controller corpse";

        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;
        public override bool AlwaysMurderer => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
        }

        public void AddArcane(Item item)
        {
            if (item is IArcaneEquip eq)
            {
                eq.CurArcaneCharges = eq.MaxArcaneCharges = 20;
            }

            item.Hue = ArcaneGem.DefaultArcaneHue;
            item.LootType = LootType.Newbied;

            AddItem(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
