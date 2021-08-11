using System;

namespace Server.Items
{
    public class RaisableItem : Item
    {
        private int m_Elevation;
        private int m_MaxElevation;
        private RaiseTimer m_RaiseTimer;

        [Constructible]
        public RaisableItem(int itemID) : this(itemID, 20, -1, -1, TimeSpan.FromMinutes(1.0))
        {
        }

        [Constructible]
        public RaisableItem(int itemID, int maxElevation, TimeSpan closeDelay) : this(
            itemID,
            maxElevation,
            -1,
            -1,
            closeDelay
        )
        {
        }

        [Constructible]
        public RaisableItem(int itemID, int maxElevation, int moveSound, int stopSound, TimeSpan closeDelay) : base(itemID)
        {
            Movable = false;

            m_MaxElevation = maxElevation;
            MoveSound = moveSound;
            StopSound = stopSound;
            CloseDelay = closeDelay;
        }

        public RaisableItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxElevation
        {
            get => m_MaxElevation;
            set
            {
                if (value <= 0)
                {
                    m_MaxElevation = 0;
                }
                else if (value >= 60)
                {
                    m_MaxElevation = 60;
                }
                else
                {
                    m_MaxElevation = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MoveSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StopSound { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan CloseDelay { get; set; }

        public bool IsRaisable => m_RaiseTimer == null;

        public void Raise()
        {
            if (!IsRaisable)
            {
                return;
            }

            m_RaiseTimer = new RaiseTimer(this);
            m_RaiseTimer.Start();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_MaxElevation);
            writer.WriteEncodedInt(MoveSound);
            writer.WriteEncodedInt(StopSound);
            writer.Write(CloseDelay);

            writer.WriteEncodedInt(m_Elevation);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_MaxElevation = reader.ReadEncodedInt();
            MoveSound = reader.ReadEncodedInt();
            StopSound = reader.ReadEncodedInt();
            CloseDelay = reader.ReadTimeSpan();

            var elevation = reader.ReadEncodedInt();
            Z -= elevation;
        }

        private class RaiseTimer : Timer
        {
            private readonly DateTime m_CloseTime;
            private readonly RaisableItem m_Item;
            private int m_Step;
            private bool m_Up;

            public RaiseTimer(RaisableItem item) : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.5))
            {
                m_Item = item;
                m_CloseTime = Core.Now + item.CloseDelay;
                m_Up = true;
            }

            protected override void OnTick()
            {
                if (m_Item.Deleted)
                {
                    Stop();
                    return;
                }

                if (m_Step++ % 3 == 0)
                {
                    if (m_Up)
                    {
                        m_Item.Z++;

                        if (++m_Item.m_Elevation >= m_Item.MaxElevation)
                        {
                            Stop();

                            if (m_Item.StopSound >= 0)
                            {
                                Effects.PlaySound(m_Item.Location, m_Item.Map, m_Item.StopSound);
                            }

                            m_Up = false;
                            m_Step = 0;

                            var delay = m_CloseTime - Core.Now;

                            StartTimer(delay, () => Start());

                            return;
                        }
                    }
                    else
                    {
                        m_Item.Z--;

                        if (--m_Item.m_Elevation <= 0)
                        {
                            Stop();

                            if (m_Item.StopSound >= 0)
                            {
                                Effects.PlaySound(m_Item.Location, m_Item.Map, m_Item.StopSound);
                            }

                            m_Item.m_RaiseTimer = null;

                            return;
                        }
                    }
                }

                if (m_Item.MoveSound >= 0)
                {
                    Effects.PlaySound(m_Item.Location, m_Item.Map, m_Item.MoveSound);
                }
            }
        }
    }
}
