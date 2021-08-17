using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class BaseAltar : Item
    {
        public enum Stage
        {

            One,
            Two,
            Three,
            Four,
            Five
        }


        private int m_AmountLoaded;
        private int m_MaxAmount;
        private bool m_CanEvolve;
        private Stage m_CurrentStage;
        private Timer m_DecayTimer;

        public event Action AmountLoadedChange;
      


        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int AmountLoaded { get => m_AmountLoaded; set { m_AmountLoaded = value; AmountLoadedChange?.Invoke(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxAmount { get => m_MaxAmount; set => m_MaxAmount = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Stage CurrentStage { get => m_CurrentStage; set { m_CurrentStage = value; } }
        public Timer DecayTimer { get => m_DecayTimer; set => m_DecayTimer = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanEvolve { get => m_CanEvolve; set => m_CanEvolve = value; }

        [Constructible]
        public BaseAltar() : base(13902)
        {

            Movable = false;
            Hue = 0;
            m_AmountLoaded = 0;
            m_MaxAmount = 200;
            m_CanEvolve = true;
            Name = "Sleeping Altar";

            AmountLoadedChange += BaseAltar_AmountLoadedChange;
            

        }

        

        public BaseAltar(Serial serial) : base(serial)
        {
        }



        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
            {
                return;
            }
            LabelTo(from, Name);
            LabelTo(from, GetLabelName(), 1161);

        }


        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {

                this.SendLocalizedMessageTo(from, 1010086);
                from.Target = new AltarTarget(this);

            }
        }

        

        private void BaseAltar_AmountLoadedChange()
        {
            // Example with using 200 max amount
            // 200
            if (m_AmountLoaded == m_MaxAmount)
            {
                // if the current stage is not max stage, set it to max stage
                // otherwise set it one step back and create flamewave
                if (m_CurrentStage < Stage.Five)
                {
                    Stage_5();
                    var towninv = new TownInvasion(InvasionTowns.Britain, TownMonsterType.Undead, TownChampionType.Barracoon, DateTime.Now);
                    towninv.OnStart();
                   // CanEvolve = false;
                }
                else
                {
                    FlameWave(this);
                    Stage_4();
                }

            }
            // 160
            else if (m_AmountLoaded >= m_MaxAmount / 5 * 4)
            {
                if (m_CurrentStage < Stage.Four)
                {
                    Stage_4();
                    //CanEvolve = false;
                }
                else
                {
                    FlameWave(this);
                    Stage_3();
                }

            }
            // 120
            else if (m_AmountLoaded >= m_MaxAmount / 5 * 3)
            {
                if (m_CurrentStage < Stage.Three)
                {
                    Stage_3();
                   // CanEvolve = false;
                }
                else
                {
                    FlameWave(this);
                    Stage_2();
                }

            }
            // 80
            else if (m_AmountLoaded >= m_MaxAmount / 5 * 2)
            {
                if (m_CurrentStage < Stage.Two)
                {
                    Stage_2();
                    //CanEvolve = false;
                }
                else
                {
                    FlameWave(this);
                    Stage_1();
                }

            }
            // 40
            else if (m_AmountLoaded >= m_MaxAmount / 5)
            {
                if (m_CurrentStage < Stage.One)
                {
                    Stage_1();
                    //CanEvolve = false;
                }
                else
                {
                    FlameWave(this);
                    Stage_0();
                    
                }

            }
            // 0
            else if (m_AmountLoaded == 0)
            {
                Stage_0();
            }
        }



        public void Stage_0()
        {
            m_CurrentStage = Stage.One;
            ItemID = 13902;
            Hue = 0;
            Name = "Sleeping Altar";


        }

        public void Stage_1()
        {
            m_CurrentStage = Stage.One;
            ItemID = 13902;
            Hue = 1161;
            Name = "Awaken Altar";


        }
        public void Stage_2()
        {
            m_CurrentStage = Stage.Two;
            ItemID = 13903;
            Hue = 0;
            Name = "Fredgling Altar";

        }
        public void Stage_3()
        {
            m_CurrentStage = Stage.Three;
            ItemID = 13903;
            Hue = 1161;
            Name = "Developing Altar";


        }
        public void Stage_4()
        {
            m_CurrentStage = Stage.Four;
            ItemID = 13904;
            Hue = 0;
            Name = "Mature Altar";

        }
        public void Stage_5()
        {

            m_CurrentStage = Stage.Five;
            ItemID = 13904;
            Hue = 1161;
            Name = "Altar of Fury";

        }

        public void Explode()
        {
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in this.GetMobilesInRange(8))
            {
                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned))
                    list.Add(m);
                else if (m.Player)
                    list.Add(m);
            }

            foreach (Mobile m in list)
            {
                //DoHarmful(m);

                m.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                m.PlaySound(0x207);
                m.SendMessage("The altar seems to be becoming unstable!");
                m.SendMessage("You take damage heavy from the exploding altar!");

                int toDamage = Utility.RandomMinMax(10, 90);
                m.Damage(toDamage);

            }
        }

        public void FlameWave(Item itm)
        {
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in this.GetMobilesInRange(8))
            {
                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned))
                    list.Add(m);
                else if (m.Player)
                    list.Add(m);
            }

            foreach (Mobile m in list)
            {
                m.SendMessage("The altar seems to be becoming unstable!");
                m.SendMessage("*Vas Grav Consume !*");
            }

            new FlameWaveTimer(itm).Start();
        }


        public virtual string GetLabelName()
        {
            return $"{m_AmountLoaded}/{m_MaxAmount}";
        }


        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(m_AmountLoaded);
            writer.Write(m_MaxAmount);
            writer.Write((int)m_CurrentStage);
            writer.Write(m_CanEvolve);
            
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_AmountLoaded = reader.ReadInt();
            m_MaxAmount = reader.ReadInt();
            m_CurrentStage = (Stage)reader.ReadInt();
            m_CanEvolve = reader.ReadBool();
        }
    }
    public class AltarTarget : Target
    {

        private BaseAltar m_BaseAltar;

        public AltarTarget(BaseAltar balt) : base(10, false, TargetFlags.None)
        {
            m_BaseAltar = balt;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            Item itm = (Item)target;

            if (target is Item)
            {
                if (itm.IsChildOf(from.Backpack))
                {
                    if (m_BaseAltar.CanEvolve)
                    {
                        if (m_BaseAltar.AmountLoaded + itm.Amount <= m_BaseAltar.MaxAmount)
                        {
                            m_BaseAltar.AmountLoaded += itm.Amount;
                            itm.Delete();
                            // Effect
                            Point3D location = m_BaseAltar.Location;
                            location.Z += 10;
                            Effects.SendLocationEffect(location, m_BaseAltar.Map, 14170, 100, 10, 0, 0);
                            Effects.PlaySound(location, m_BaseAltar.Map, 0x32E);
                            //Decay Timer
                            if (m_BaseAltar.DecayTimer != null)
                            {
                                //Timer.TimerThread.RemoveTimer(m_BaseAltar.DecayTimer);
                                m_BaseAltar.DecayTimer.Stop();
                            }
                            m_BaseAltar.DecayTimer = Timer.DelayCall(
                                TimeSpan.FromSeconds(10),
                                TimeSpan.FromSeconds(10),
                                delegate ()
                                {
                                    if (m_BaseAltar.AmountLoaded > m_BaseAltar.MaxAmount / 5)
                                    {
                                        m_BaseAltar.AmountLoaded = m_BaseAltar.AmountLoaded - m_BaseAltar.MaxAmount / 5;

                                    }

                                });
                        }
                        else
                        {
                            from.SendMessage("The maximum ressource amount has been reached.");
                        }
                    }
                    else
                    {
                        from.SendMessage("The altar seems stable at the moment and cannot evolve.");
                    }

                    

                }
                else
                    from.SendLocalizedMessage(1042001);

            }
            else
            {
                from.SendMessage("This is not a sacrificial ressource.");
            }


        }
    }



    internal class FlameWaveTimer : Timer
    {
        private Item m_From;
        private Point3D m_StartingLocation;
        private Map m_Map;
        private int m_Count;
        private Point3D m_Point;

        public FlameWaveTimer(Item from)
            : base(TimeSpan.FromMilliseconds(300.0), TimeSpan.FromMilliseconds(300.0))
        {
            m_From = from;
            m_StartingLocation = from.Location;
            m_Map = from.Map;
            m_Count = 0;
            m_Point = new Point3D();
            SetupDamage(from);
        }

        protected override void OnTick()
        {
            if (m_From == null || m_From.Deleted)
            {
                Stop();
                return;
            }

            double dist = 0.0;

            for (int i = -m_Count; i < m_Count + 1; i++)
            {
                for (int j = -m_Count; j < m_Count + 1; j++)
                {
                    m_Point.X = m_StartingLocation.X + i;
                    m_Point.Y = m_StartingLocation.Y + j;
                    m_Point.Z = m_Map.GetAverageZ(m_Point.X, m_Point.Y);
                    dist = GetDist(m_StartingLocation, m_Point);
                    if (dist < ((double)m_Count + 0.1) && dist > ((double)m_Count - 3.1))
                    {
                        Effects.SendLocationParticles(EffectItem.Create(m_Point, m_Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                    }
                }
            }

            m_Count += 3;

            if (m_Count > 15)
                Stop();
        }

        private void SetupDamage(Item from)
        {
            foreach (Mobile m in from.GetMobilesInRange(12))
            {
                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned) || m.Player)
                {
                    //Timer.DelayCall(TimeSpan.FromMilliseconds(300 * (GetDist(m_StartingLocation, m.Location) / 3)), new TimerStateCallback<object>(Hurt), m);
                    Timer.DelayCall(TimeSpan.FromMilliseconds(300 * (GetDist(m_StartingLocation, m.Location) / 3)), () => Hurt(m));

                }
            }
        }

        public void Hurt(object o)
        {
            Mobile m = o as Mobile;

            if (m_From == null || m == null || m.Deleted)
                return;

            int toDamage = Utility.RandomMinMax(10, 90);
            m.Damage(toDamage);

            m.SendMessage("You are being burnt alive by the seering heat!");
        }
        private double GetDist(Point3D start, Point3D end)
        {
            int xdiff = start.X - end.X;
            int ydiff = start.Y - end.Y;
            return Math.Sqrt((xdiff * xdiff) + (ydiff * ydiff));
        }
    }
}

