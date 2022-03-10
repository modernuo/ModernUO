using Server.Items;

namespace Server.Mobiles
{
    public class DreadSpider : BaseCreature
    {
        [Constructible]
        public DreadSpider() : base(AIType.AI_Mage)
        {
            Body = 11;
            BaseSoundID = 1170;

            SetStr(196, 220);
            SetDex(126, 145);
            SetInt(286, 310);

            SetHits(118, 132);

            SetDamage(5, 17);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Poison, 80);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 90, 100);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.EvalInt, 65.1, 80.0);
            SetSkill(SkillName.Magery, 65.1, 80.0);
            SetSkill(SkillName.Meditation, 65.1, 80.0);
            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 55.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 75.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 36;

            PackItem(new SpidersSilk(8));
        }

        public DreadSpider(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a dread spider corpse";
        public override string DefaultName => "a dread spider";

        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;
        public override int TreasureMapLevel => 3;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
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

            if (BaseSoundID == 263)
            {
                BaseSoundID = 1170;
            }
        }
    }
}
