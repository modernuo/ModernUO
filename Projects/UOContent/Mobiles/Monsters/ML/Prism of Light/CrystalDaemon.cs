namespace Server.Mobiles
{
    public class CrystalDaemon : BaseCreature
    {
        [Constructible]
        public CrystalDaemon()
            : base(AIType.AI_Mage)
        {
            Body = 0x310;
            Hue = 0x3E8;
            BaseSoundID = 0x47D;

            SetStr(140, 200);
            SetDex(120, 150);
            SetInt(800, 850);

            SetHits(200, 220);

            SetDamage(16, 20);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 40);
            SetDamageType(ResistanceType.Energy, 60);

            SetResistance(ResistanceType.Physical, 20, 40);
            SetResistance(ResistanceType.Fire, 0, 20);
            SetResistance(ResistanceType.Cold, 60, 80);
            SetResistance(ResistanceType.Poison, 20, 40);
            SetResistance(ResistanceType.Energy, 65, 75);

            SetSkill(SkillName.Wrestling, 60.0, 80.0);
            SetSkill(SkillName.Tactics, 70.0, 80.0);
            SetSkill(SkillName.MagicResist, 100.0, 110.0);
            SetSkill(SkillName.Magery, 120.0, 130.0);
            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Meditation, 100.0, 110.0);

            Fame = 15000;
            Karma = -15000;

            PackArcaneScroll(0, 1);
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.4)
            c.DropItem( new ScatteredCrystals() );
        }
        */

        public CrystalDaemon(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a crystal daemon corpse";
        public override string DefaultName => "a crystal daemon";

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 3);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
