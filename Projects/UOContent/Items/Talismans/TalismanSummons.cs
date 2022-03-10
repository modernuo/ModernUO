using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;

namespace Server.Mobiles
{
    public class BaseTalismanSummon : BaseCreature
    {
        // public override bool IsInvulnerable => true; // TODO: Wailing banshees are NOT invulnerable, are any of the others?

        public BaseTalismanSummon() : base(AIType.AI_Melee, FightMode.None)
        {
            // TODO: Stats/skills
        }

        public BaseTalismanSummon(Serial serial) : base(serial)
        {
        }

        public override bool Commandable => false;
        public override bool InitialInnocent => true;

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from.Alive && ControlMaster == from)
            {
                list.Add(new TalismanReleaseEntry(this));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        private class TalismanReleaseEntry : ContextMenuEntry
        {
            private readonly Mobile m_Mobile;

            public TalismanReleaseEntry(Mobile m) : base(6118, 3) => m_Mobile = m;

            public override void OnClick()
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(m_Mobile.Location, m_Mobile.Map, EffectItem.DefaultDuration),
                    0x3728,
                    8,
                    20,
                    5042
                );

                Effects.PlaySound(m_Mobile,0x201);

                m_Mobile.Delete();
            }
        }
    }

    public class SummonedAntLion : BaseTalismanSummon
    {
        [Constructible]
        public SummonedAntLion()
        {
            Body = 787;
            BaseSoundID = 1006;
        }

        public SummonedAntLion(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "an ant lion";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedArcticOgreLord : BaseTalismanSummon
    {
        [Constructible]
        public SummonedArcticOgreLord()
        {
            Body = 135;
            BaseSoundID = 427;
        }

        public SummonedArcticOgreLord(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "an arctic ogre lord";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBakeKitsune : BaseTalismanSummon
    {
        [Constructible]
        public SummonedBakeKitsune()
        {
            Body = 246;
            BaseSoundID = 0x4DD;
        }

        public SummonedBakeKitsune(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a bake kitsune";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBogling : BaseTalismanSummon
    {
        [Constructible]
        public SummonedBogling()
        {
            Body = 779;
            BaseSoundID = 422;
        }

        public SummonedBogling(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a bogling";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBullFrog : BaseTalismanSummon
    {
        [Constructible]
        public SummonedBullFrog()
        {
            Body = 81;
            Hue = Utility.RandomList(0x5AC, 0x5A3, 0x59A, 0x591, 0x588, 0x57F);
            BaseSoundID = 0x266;
        }

        public SummonedBullFrog(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a bull frog";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedChicken : BaseTalismanSummon
    {
        [Constructible]
        public SummonedChicken()
        {
            Body = 0xD0;
            BaseSoundID = 0x6E;
        }

        public SummonedChicken(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a chicken";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedCow : BaseTalismanSummon
    {
        [Constructible]
        public SummonedCow()
        {
            Body = Utility.RandomList(0xD8, 0xE7);
            BaseSoundID = 0x78;
        }

        public SummonedCow(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a cow";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedDoppleganger : BaseTalismanSummon
    {
        [Constructible]
        public SummonedDoppleganger()
        {
            Body = 0x309;
            BaseSoundID = 0x451;
        }

        public SummonedDoppleganger(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a doppleganger";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedFrostSpider : BaseTalismanSummon
    {
        [Constructible]
        public SummonedFrostSpider()
        {
            Body = 20;
            BaseSoundID = 0x388;
        }

        public SummonedFrostSpider(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a frost spider";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedGreatHart : BaseTalismanSummon
    {
        [Constructible]
        public SummonedGreatHart()
        {
            Body = 0xEA;
            BaseSoundID = 0x82;
        }

        public SummonedGreatHart(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a great hart";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedLavaSerpent : BaseTalismanSummon
    {
        [Constructible]
        public SummonedLavaSerpent()
        {
            Body = 90;
            BaseSoundID = 219;
        }

        public SummonedLavaSerpent(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a lava serpent";

        public override void OnThink()
        {
            /*
            if (m_NextWave < Core.Now)
              AreaHeatDamage();
            */
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        /*
        // An area attack that only damages staff, wtf?

        private DateTime m_NextWave;

        public void AreaHeatDamage()
        {
          Mobile mob = ControlMaster;

          if (mob != null)
          {
            if (mob.InRange( Location, 2 ))
            {
              if (mob.AccessLevel != AccessLevel.Player)
              {
                AOS.Damage( mob, Utility.Random( 2, 3 ), 0, 100, 0, 0, 0 );
                mob.SendLocalizedMessage( 1008112 ); // The intense heat is damaging you!
              }
            }

            GuardedRegion r = Region as GuardedRegion;

            if (r != null && mob.Alive)
            {
              foreach ( Mobile m in GetMobilesInRange( 2 ) )
              {
                if (!mob.CanBeHarmful( m ))
                  mob.CriminalAction( false );
              }
            }
          }

          m_NextWave = Core.Now + TimeSpan.FromSeconds( 3 );
        }
        */
    }

    public class SummonedOrcBrute : BaseTalismanSummon
    {
        [Constructible]
        public SummonedOrcBrute()
        {
            Body = 189;
            BaseSoundID = 0x45A;
        }

        public SummonedOrcBrute(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "an orc brute";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedPanther : BaseTalismanSummon
    {
        [Constructible]
        public SummonedPanther()
        {
            Body = 0xD6;
            Hue = 0x901;
            BaseSoundID = 0x462;
        }

        public SummonedPanther(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a panther";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedSheep : BaseTalismanSummon
    {
        [Constructible]
        public SummonedSheep()
        {
            Body = 0xCF;
            BaseSoundID = 0xD6;
        }

        public SummonedSheep(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a sheep";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedSkeletalKnight : BaseTalismanSummon
    {
        [Constructible]
        public SummonedSkeletalKnight()
        {
            Body = 147;
            BaseSoundID = 451;
        }

        public SummonedSkeletalKnight(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a skeletal knight";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedVorpalBunny : BaseTalismanSummon
    {
        [Constructible]
        public SummonedVorpalBunny()
        {
            Body = 205;
            Hue = 0x480;
            BaseSoundID = 0xC9;

            Timer.StartTimer(TimeSpan.FromMinutes(30.0), BeginTunnel);
        }

        public SummonedVorpalBunny(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a vorpal bunny";

        public virtual void BeginTunnel()
        {
            if (Deleted)
            {
                return;
            }

            new VorpalBunny.BunnyHole().MoveToWorld(Location, Map);

            Frozen = true;
            Say("* The bunny begins to dig a tunnel back to its underground lair *");
            PlaySound(0x247);

            Timer.StartTimer(TimeSpan.FromSeconds(5.0), Delete);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SummonedWailingBanshee : BaseTalismanSummon
    {
        [Constructible]
        public SummonedWailingBanshee()
        {
            Body = 310;
            BaseSoundID = 0x482;
        }

        public SummonedWailingBanshee(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a wailing banshee";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
