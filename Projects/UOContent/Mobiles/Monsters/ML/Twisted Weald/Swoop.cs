using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Swoop : Eagle
    {
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        [Constructible]
        public Swoop()
        {
            IsParagon = true;
            Hue = 0xE0;

            AI = AIType.AI_Melee;

            SetStr(100, 150);
            SetDex(400, 500);
            SetInt(80, 90);

            SetHits(1500, 2000);

            SetDamage(20, 30);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 75, 90);
            SetResistance(ResistanceType.Fire, 60, 77);
            SetResistance(ResistanceType.Cold, 70, 85);
            SetResistance(ResistanceType.Poison, 55, 85);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.Wrestling, 120.0, 140.0);
            SetSkill(SkillName.Tactics, 120.0, 140.0);
            SetSkill(SkillName.MagicResist, 95.0, 105.0);

            Fame = 18000;
            Karma = 0;

            PackReg(4);
            PackArcaneScroll(0, 1);
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.025)
          {
            switch ( Utility.Random( 18 ) )
            {
              case 0: c.DropItem( new AssassinChest() ); break;
              case 1: c.DropItem( new AssassinArms() ); break;
              case 2: c.DropItem( new DeathChest() ); break;
              case 3: c.DropItem( new MyrmidonArms() ); break;
              case 4: c.DropItem( new MyrmidonLegs() ); break;
              case 5: c.DropItem( new MyrmidonGorget() ); break;
              case 6: c.DropItem( new LeafweaveGloves() ); break;
              case 7: c.DropItem( new LeafweaveLegs() ); break;
              case 8: c.DropItem( new LeafweavePauldrons() ); break;
              case 9: c.DropItem( new PaladinGloves() ); break;
              case 10: c.DropItem( new PaladinGorget() ); break;
              case 11: c.DropItem( new PaladinArms() ); break;
              case 12: c.DropItem( new HunterArms() ); break;
              case 13: c.DropItem( new HunterGloves() ); break;
              case 14: c.DropItem( new HunterLegs() ); break;
              case 15: c.DropItem( new HunterChest() ); break;
              case 16: c.DropItem( new GreymistArms() ); break;
              case 17: c.DropItem( new GreymistGloves() ); break;
            }
          }

          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public Swoop(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a Swoop corpse";
        public override string DefaultName => "Swoop";

        public override bool CanFly => true;
        public override bool GivesMLMinorArtifact => true;
        public override int Feathers => 72;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        // TODO: Put this attack shared with Hiryu and Lesser Hiryu in one place
        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            if (Utility.RandomDouble() < 0.9)
            {
                return;
            }

            if (m_Table.Remove(defender, out var timer))
            {
                timer.DoExpire();
                defender.SendLocalizedMessage(1070837); // The creature lands another blow in your weakened state.
            }
            else
            {
                // The blow from the creature's claws has made you more susceptible to physical attacks.
                defender.SendLocalizedMessage(1070836);
            }

            var effect = -(defender.PhysicalResistance * 15 / 100);

            var mod = new ResistanceMod(ResistanceType.Physical, "PhysicalResistGraspingClaw", effect);

            defender.FixedEffect(0x37B9, 10, 5);
            defender.AddResistanceMod(mod);

            timer = new ExpireTimer(defender, mod, TimeSpan.FromSeconds(5.0));
            timer.Start();
            m_Table[defender] = timer;
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

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly ResistanceMod m_Mod;

            public ExpireTimer(Mobile m, ResistanceMod mod, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
                m_Mod = mod;
            }

            public void DoExpire()
            {
                m_Mobile.RemoveResistanceMod(m_Mod);
                Stop();
            }

            protected override void OnTick()
            {
                m_Mobile.SendLocalizedMessage(1070838); // Your resistance to physical attacks has returned.
                DoExpire();
                m_Table.Remove(m_Mobile);
            }
        }
    }
}
