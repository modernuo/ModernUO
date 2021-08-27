using System;

namespace Server.Items
{
    [Serializable(0)]
    public partial class ElvenSpinningwheelSouthAddon : BaseAddon, ISpinningWheel
    {
        private Timer m_Timer;

        [Constructible]
        public ElvenSpinningwheelSouthAddon()
        {
            AddComponent(new AddonComponent(0x2DDA), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new ElvenSpinningwheelSouthDeed();

        public bool Spinning => m_Timer != null;

        public void BeginSpin(SpinCallback callback, Mobile from, int hue)
        {
            m_Timer = new SpinTimer(this, callback, from, hue);
            m_Timer.Start();

            foreach (var c in Components)
            {
                switch (c.ItemID)
                {
                    case 0x1015:
                    case 0x1019:
                    case 0x101C:
                    case 0x10A4:
                        ++c.ItemID;
                        break;
                }
            }
        }

        public override void OnComponentLoaded(AddonComponent c)
        {
            switch (c.ItemID)
            {
                case 0x1016:
                case 0x101A:
                case 0x101D:
                case 0x10A5:
                    --c.ItemID;
                    break;
            }
        }

        public void EndSpin(SpinCallback callback, Mobile from, int hue)
        {
            m_Timer?.Stop();

            m_Timer = null;

            foreach (var c in Components)
            {
                switch (c.ItemID)
                {
                    case 0x1016:
                    case 0x101A:
                    case 0x101D:
                    case 0x10A5:
                        --c.ItemID;
                        break;
                }
            }

            callback?.Invoke(this, from, hue);
        }

        private class SpinTimer : Timer
        {
            private readonly SpinCallback m_Callback;
            private readonly Mobile m_From;
            private readonly int m_Hue;
            private readonly ElvenSpinningwheelSouthAddon m_Wheel;

            public SpinTimer(ElvenSpinningwheelSouthAddon wheel, SpinCallback callback, Mobile from, int hue) : base(
                TimeSpan.FromSeconds(3.0)
            )
            {
                m_Wheel = wheel;
                m_Callback = callback;
                m_From = from;
                m_Hue = hue;
            }

            protected override void OnTick()
            {
                m_Wheel.EndSpin(m_Callback, m_From, m_Hue);
            }
        }
    }

    [Serializable(0)]
    public partial class ElvenSpinningwheelSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public ElvenSpinningwheelSouthDeed()
        {
        }

        public override BaseAddon Addon => new ElvenSpinningwheelSouthAddon();
        public override int LabelNumber => 1072878; // spinning wheel (south)
    }
}
