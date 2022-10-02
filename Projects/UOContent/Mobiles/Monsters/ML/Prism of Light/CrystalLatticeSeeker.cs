namespace Server.Mobiles
{
    public class CrystalLatticeSeeker : BaseCreature
    {
        [Constructible]
        public CrystalLatticeSeeker()
            : base(AIType.AI_Mage)
        {
            Body = 0x7B;
            Hue = 0x47E;

            SetStr(550, 850);
            SetDex(190, 250);
            SetInt(350, 450);

            SetHits(350, 550);

            SetDamage(13, 19);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 80, 90);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Anatomy, 50.0, 75.0);
            SetSkill(SkillName.EvalInt, 90.0, 100.0);
            SetSkill(SkillName.Magery, 100.0, 100.0);
            SetSkill(SkillName.Meditation, 90.0, 100.0);
            SetSkill(SkillName.MagicResist, 90.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);

            Fame = 17000;
            Karma = -17000;

            PackArcaneScroll(0, 2);
        }

        public CrystalLatticeSeeker(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a Crystal Lattice Seeker corpse";
        public override string DefaultName => "Crystal Lattice Seeker";

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.75)
            c.DropItem( new CrystallineFragments() );

          if (Utility.RandomDouble() < 0.07)
            c.DropItem( new PiecesOfCrystal() );
        }
        */

        public override int Feathers => 100;
        public override int TreasureMapLevel => 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 4);
            // TODO: uncomment once added
            // AddLoot( LootPack.Parrot );
            AddLoot(LootPack.Gems);
        }

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            if (Utility.RandomDouble() < 0.1)
            {
                Drain(defender);
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            if (Utility.RandomDouble() < 0.1)
            {
                Drain(attacker);
            }
        }

        public virtual void Drain(Mobile m)
        {
            int toDrain;

            switch (Utility.Random(3))
            {
                case 0:
                    {
                        Say(1042156); // I can grant life, and I can sap it as easily.
                        PlaySound(0x1E6);

                        toDrain = Utility.RandomMinMax(3, 6);
                        Hits += toDrain;
                        m.Hits -= toDrain;
                        break;
                    }
                case 1:
                    {
                        Say(1042157); // You'll go nowhere, unless I deem it should be so.
                        PlaySound(0x1DF);

                        toDrain = Utility.RandomMinMax(10, 25);
                        Stam += toDrain;
                        m.Stam -= toDrain;
                        break;
                    }
                case 2:
                    {
                        Say(1042155); // Your power is mine to use as I will.
                        PlaySound(0x1F8);

                        toDrain = Utility.RandomMinMax(15, 25);
                        Mana += toDrain;
                        m.Mana -= toDrain;
                        break;
                    }
            }
        }

        public override int GetAttackSound() => 0x2F6;

        public override int GetDeathSound() => 0x2F7;

        public override int GetAngerSound() => 0x2F8;

        public override int GetHurtSound() => 0x2F9;

        public override int GetIdleSound() => 0x2FA;

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
