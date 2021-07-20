using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Server.Items;
using Server.Targeting;
using Server.Multis;

namespace Server.Mobiles
{
    public class PirateCaptain : BaseCreature
    {
        private PirateShip_Boat m_PirateShip_Boat;

        #region Pirate Can Say Random Phrases From A [.txt] File

        public bool active;

        public static string path = "Data/The Pirate Captain/Speech.txt";

        private DateTime nextAbilityTime;

        private StreamReader text;

        private string curspeech;

        public override bool InitialInnocent { get { return true; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (!value)
                {
                    CloseStream();
                }

                active = value;
            }
        }

        #endregion Pirate Can Say Random Phrases From A [.txt] File

        [Constructible]
        public PirateCaptain() : base(AIType.AI_Archer, FightMode.Closest, 15, 1, 0.2, 0.4)
        {

            Hue = 0;

            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }

            Title = "[Captain]";

            #region Pirate Can Say Random Phrases From A [.txt] File

            SpeechHue = Utility.RandomDyedHue();

            #endregion Pirate Can Say Random Phrases From A [.txt] File

            AddItem(new ThighBoots());
            AddItem(new TricorneHat(Utility.RandomRedHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            if (Utility.RandomBool() && !this.Female)
            {
                Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));

                beard.Hue = hair.Hue;
                beard.Layer = Layer.FacialHair;
                beard.Movable = false;

                AddItem(beard);

                Item necklace = new Necklace();
                necklace.Name = "A Pirates Medallion";
                necklace.Movable = false;
                necklace.Hue = 38;

                AddItem(necklace);
            }

            SetStr(195, 200);
            SetDex(181, 195);
            SetInt(61, 75);
            SetHits(288, 308);

            SetDamage(20, 40);

            SetDamageType(ResistanceType.Physical, 100, 100);
            SetDamageType(ResistanceType.Fire, 25, 50);
            SetDamageType(ResistanceType.Cold, 25, 50);
            SetDamageType(ResistanceType.Energy, 25, 50);
            SetDamageType(ResistanceType.Poison, 25, 50);

            SetResistance(ResistanceType.Physical, 100, 100);
            SetResistance(ResistanceType.Fire, 25, 50);
            SetResistance(ResistanceType.Cold, 25, 50);
            SetResistance(ResistanceType.Energy, 25, 50);
            SetResistance(ResistanceType.Poison, 25, 50);

            SetSkill(SkillName.Fencing, 86.0, 97.5);
            SetSkill(SkillName.Macing, 85.0, 87.5);
            SetSkill(SkillName.MagicResist, 55.0, 67.5);
            SetSkill(SkillName.Swords, 85.0, 87.5);
            SetSkill(SkillName.Tactics, 85.0, 87.5);
            SetSkill(SkillName.Wrestling, 35.0, 37.5);
            SetSkill(SkillName.Archery, 85.0, 87.5);

            #region Pirate Can Say Random Phrases From A [.txt] File

            active = true;

            #endregion Pirate Can Say Random Phrases From A [.txt] File

            CantWalk = false;

            Fame = 5000;
            Karma = -5000;
            VirtualArmor = 66;
            {

                switch (Utility.Random(1))
                {
                    case 0: AddItem(new LongPants(Utility.RandomRedHue())); break;
                    case 1: AddItem(new ShortPants(Utility.RandomRedHue())); break;
                }

                switch (Utility.Random(3))
                {
                    case 0: AddItem(new FancyShirt(Utility.RandomRedHue())); break;
                    case 1: AddItem(new Shirt(Utility.RandomRedHue())); break;
                    case 2: AddItem(new Doublet(Utility.RandomRedHue())); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new Bow()); break;
                    case 1: AddItem(new CompositeBow()); break;
                    case 2: AddItem(new Crossbow()); break;
                    case 3: AddItem(new RepeatingCrossbow()); break;
                    case 4: AddItem(new HeavyCrossbow()); break;
                }
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
        }

        public override bool IsScaredOfScaryThings { get { return false; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }
        public override bool AutoDispel { get { return true; } }
        public override bool CanRummageCorpses { get { return true; } }

        #region Pirate Can Say Random Phrases From A [.txt] File

        public void Emote()
        {
            switch (Utility.Random(85))
            {
                case 1:
                    PlaySound(Female ? 785 : 1056);
                    Say("*cough!*");
                    break;
                case 2:
                    PlaySound(Female ? 818 : 1092);
                    Say("*sniff*");
                    break;
                default:
                    break;
            }
        }

        public void CloseStream()
        {
            if (text != null)
            {
                try { text.Close(); text = null; }
                catch { };
            }
        }

        public void Talk()
        {
            if (text == null) return;

            try
            {
                curspeech = text.ReadLine();

                if (curspeech == null) throw (new ArgumentNullException());

                Say(curspeech);
            }
            catch
            {
                CloseStream();
            }
        }

        public override void OnDeath(Container c)
        {
            CloseStream();
            base.OnDeath(c);
        }

        #endregion Pirate Can Say Random Phrases From A [.txt] File

        public override bool PlayerRangeSensitive { get { return false; } }

        private bool boatspawn;
        private DateTime m_NextPickup;
        private BaseBoat m_enemyboat;
        private Direction enemydirection;

        public override void OnThink()
        {

            #region Pirate Can Say Random Phrases From A [.txt] File

            if (DateTime.Now >= nextAbilityTime && Combatant == null && active == true)
            {
                nextAbilityTime = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(4, 6));

                if (text == null)
                {
                    try
                    {
                        text = new StreamReader(path, System.Text.Encoding.Default, false);
                    }
                    catch { }
                }
                Talk();
                Emote();
            }

            #endregion Pirate Can Say Random Phrases From A [.txt] File

            if (boatspawn == false)
            {
                Map map = this.Map;
                if (map == null)
                    return;
                this.Z = 0;
                m_PirateShip_Boat = new PirateShip_Boat();
                Point3D loc = this.Location;
                Point3D loccrew = this.Location;

                loc = new Point3D(this.X, this.Y - 1, this.Z);
                loccrew = new Point3D(this.X, this.Y - 1, this.Z + 1);

                m_PirateShip_Boat.MoveToWorld(loc, map);
                boatspawn = true;

                for (int i = 0; i < 5; ++i)
                {
                    PirateCrew m_PirateCrew = new PirateCrew();
                    m_PirateCrew.MoveToWorld(loccrew, map);
                }
            }

            base.OnThink();
            if (DateTime.Now < m_NextPickup)
                return;

            if (m_PirateShip_Boat == null)
            {
                return;
            }

            m_NextPickup = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(1, 2));

            enemydirection = Direction.North;
            foreach (Item enemy in this.GetItemsInRange(200))
            {
                if (enemy is BaseBoat && enemy != m_PirateShip_Boat && !(enemy is PirateShip_Boat))
                {
                    List<Mobile> targets = new List<Mobile>();
                    IPooledEnumerable eable = enemy.GetMobilesInRange(16);

                    foreach (Mobile m in eable)
                    {
                        if (m is PlayerMobile)
                            targets.Add(m);
                    }
                    eable.Free();
                    if (targets.Count > 0)
                    {
                        m_enemyboat = enemy as BaseBoat;
                        enemydirection = this.GetDirectionTo(m_enemyboat);
                        break;
                    }
                }
            }
            if (m_enemyboat == null)
            {
                return;
            }

            if (m_PirateShip_Boat != null && m_enemyboat != null)
            {
                if (m_PirateShip_Boat != null && (enemydirection == Direction.North) && m_PirateShip_Boat.Facing != Direction.North)
                {
                    m_PirateShip_Boat.Facing = Direction.North;
                }
                else if (m_PirateShip_Boat != null && (enemydirection == Direction.South) && m_PirateShip_Boat.Facing != Direction.South)
                {
                    m_PirateShip_Boat.Facing = Direction.South;
                }
                else if (m_PirateShip_Boat != null && (enemydirection == Direction.East || enemydirection == Direction.Right || enemydirection == Direction.Down) && m_PirateShip_Boat.Facing != Direction.East)
                {
                    m_PirateShip_Boat.Facing = Direction.East;
                }
                else if (m_PirateShip_Boat != null && (enemydirection == Direction.West || enemydirection == Direction.Left || enemydirection == Direction.Up) && m_PirateShip_Boat.Facing != Direction.West)
                {
                    m_PirateShip_Boat.Facing = Direction.West;
                }
                m_PirateShip_Boat.StartMove(Direction.North, true);

                if (m_PirateShip_Boat != null && this.InRange(m_enemyboat, 10) && m_PirateShip_Boat.IsMoving == true)
                {
                    m_PirateShip_Boat.StopMove(false);
                }
            }
            else
            {
                if (m_PirateShip_Boat != null && m_PirateShip_Boat.IsMoving == true)
                {
                    m_PirateShip_Boat.StopMove(false);
                }
            }
        }
        public override void OnDelete()
        {
            if (m_PirateShip_Boat != null)
            {
                new SinkTimer(m_PirateShip_Boat, this).Start();

                #region Pirate Can Say Random Phrases From A [.txt] File

            }
            {
                CloseStream();
                base.OnDelete();

                #endregion Pirate Can Say Random Phrases From A [.txt] File

            }
        }

        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            SpawnPirate(caster);
        }

        public void SpawnPirate(Mobile target)
        {
            Map map = target.Map;

            if (map == null)
                return;

            int pirates = 0;

            foreach (Mobile m in this.GetMobilesInRange(10))
            {
                if (m is PirateCrew)
                    ++pirates;
            }

            if (pirates < 10 && Utility.RandomDouble() <= 0.25)
            {
                BaseCreature PirateCrew = new PirateCrew();

                Point3D loc = target.Location;
                bool validLocation = false;

                for (int j = 0; !validLocation && j < 10; ++j)
                {
                    int x = target.X + Utility.Random(3) - 1;
                    int y = target.Y + Utility.Random(3) - 1;
                    int z = map.GetAverageZ(x, y);

                    if (validLocation = map.CanFit(x, y, this.Z, 16, false, false))
                        loc = new Point3D(x, y, Z);
                    else if (validLocation = map.CanFit(x, y, z, 16, false, false))
                        loc = new Point3D(x, y, z);
                }

                PirateCrew.MoveToWorld(loc, map);

                PirateCrew.Combatant = target;
            }
        }

        public override bool OnBeforeDeath()
        {
            if (m_PirateShip_Boat != null)
            {
                new SinkTimer(m_PirateShip_Boat, this).Start();
            }
            return true;
        }
        private class SinkTimer : Timer
        {
            private BaseBoat m_Boat;
            private int m_Count;
            private Mobile m_mobile;

            public SinkTimer(BaseBoat boat, Mobile m) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(4.0))
            {
                m_Boat = boat;
                m_mobile = m;

                //Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                if (m_Count == 4)
                {
                    List<Mobile> targets = new List<Mobile>();
                    IPooledEnumerable eable = m_Boat.GetMobilesInRange(16);

                    foreach (Mobile m in eable)
                    {
                        if (m is PirateCrew)
                            targets.Add(m);
                    }
                    eable.Free();
                    if (targets.Count > 0)
                    {
                        for (int i = 0; i < targets.Count; ++i)
                        {
                            Mobile m = targets[i];
                            m.Kill();
                        }
                    }
                }
                if (m_Count >= 15)
                {
                    m_Boat.Delete();
                    Stop();
                }
                else
                {
                    if (m_Count < 5)
                    {
                        m_Boat.Location = new Point3D(m_Boat.X, m_Boat.Y, m_Boat.Z - 1);

                        if (m_Boat.TillerMan != null && m_Count < 5)
                            m_Boat.TillerMan.Say(1007168 + m_Count);
                    }
                    else
                    {
                        m_Boat.Location = new Point3D(m_Boat.X, m_Boat.Y, m_Boat.Z - 3);

                    }
                    ++m_Count;
                }
            }
        }

        public PirateCaptain(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((Item)m_PirateShip_Boat);
            writer.Write((bool)boatspawn);
            writer.Write((int)0);

            #region Pirate Can Say Random Phrases From A [.txt] File

            writer.Write((bool)active);

            #endregion Pirate Can Say Random Phrases From A [.txt] File

        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            m_PirateShip_Boat = reader.ReadEntity<PirateShip_Boat>();
            boatspawn = reader.ReadBool();
            int version = reader.ReadInt();

            #region Pirate Can Say Random Phrases From A [.txt] File

            active = reader.ReadBool();

            #endregion Pirate Can Say Random Phrases From A [.txt] File

        }
    }
}
