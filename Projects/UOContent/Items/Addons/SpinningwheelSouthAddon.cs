using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.SpinningwheelSouthAddon")]
    public partial class SpinningWheelSouthAddon : BaseAddon, ISpinningWheel
    {
        private Timer m_Timer;

        [Constructible]
        public SpinningWheelSouthAddon()
        {
            AddComponent(new AddonComponent(0x1015), 0, 0, 0);
        }

        public override int LabelNumber => 1024117; // spinning wheel
        public override BaseAddonDeed Deed => new SpinningWheelSouthDeed();

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
            private readonly SpinningWheelSouthAddon m_Wheel;

            public SpinTimer(SpinningWheelSouthAddon wheel, SpinCallback callback, Mobile from, int hue) : base(
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

    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.SpinningwheelSouthDeed")]
    public partial class SpinningWheelSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SpinningWheelSouthDeed()
        {
        }

        public override BaseAddon Addon => new SpinningWheelSouthAddon();
        public override int LabelNumber => 1044342; // spinning wheel (south)
    }
}
