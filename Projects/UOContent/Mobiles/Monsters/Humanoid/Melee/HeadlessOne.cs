namespace Server.Mobiles
{
    public class HeadlessOne : BaseCreature
    {
        [Constructible]
        public HeadlessOne() : base(AIType.AI_Melee)
        {
            Body = 31;
            Hue = Race.Human.RandomSkinHue() & 0x7FFF;
            BaseSoundID = 0x39D;

            SetStr(26, 50);
            SetDex(36, 55);
            SetInt(16, 30);

            SetHits(16, 30);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 25.1, 40.0);
            SetSkill(SkillName.Wrestling, 25.1, 40.0);

            Fame = 450;
            Karma = -450;

            VirtualArmor = 18;
        }

        public HeadlessOne(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a headless corpse";
        public override string DefaultName => "a headless one";

        public override bool CanRummageCorpses => true;
        public override int Meat => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
            // TODO: body parts
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
