namespace Server.Mobiles
{
    public class CrystalVortex : BaseCreature
    {
        [Constructible]
        public CrystalVortex()
            : base(AIType.AI_Melee)
        {
            Body = 0xD;
            Hue = 0x2B2;
            BaseSoundID = 0x107;

            SetStr(800, 900);
            SetDex(500, 600);
            SetInt(200);

            SetHits(350, 400);
            SetMana(0);

            SetDamage(15, 20);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 60, 80);
            SetResistance(ResistanceType.Fire, 0, 10);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 60, 90);

            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);

            Fame = 17000;
            Karma = -17000;

            PackArcaneScroll(0, 2);
        }

        public CrystalVortex(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a crystal vortex corpse";
        public override string DefaultName => "a crystal vortex";

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            // TODO: uncomment once added
            // AddLoot( LootPack.Parrot );
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.75)
            c.DropItem( new CrystallineFragments() );

          if (Utility.RandomDouble() < 0.06)
            c.DropItem( new JaggedCrystals() );
        }
        */

        public override int GetAngerSound() => 0x15;

        public override int GetAttackSound() => 0x28;

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
