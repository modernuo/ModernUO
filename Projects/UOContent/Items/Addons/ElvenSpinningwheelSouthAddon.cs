using System;

namespace Server.Items
{
<<<<<<< HEAD
    [Serializable(0, false)]
    public partial class ElvenSpinningwheelSouthAddon : BaseAddon, ISpinningWheel
=======
    [Serializable(0)]
    [TypeAlias("Server.Items.ElvenSpinningwheelSouthAddon")]
    public partial class ElvenSpinningWheelSouthAddon : BaseAddon, ISpinningWheel
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    {
        private Timer m_Timer;

        [Constructible]
        public ElvenSpinningWheelSouthAddon()
        {
            AddComponent(new AddonComponent(0x2E3F), 0, 0, 0);
        }

        public override int LabelNumber => 1031737; // elven spinning wheel
<<<<<<< HEAD
        public override BaseAddonDeed Deed => new ElvenSpinningwheelSouthDeed();
=======
        public override BaseAddonDeed Deed => new ElvenSpinningWheelSouthDeed();
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8

        public bool Spinning => m_Timer != null;

        public void BeginSpin(SpinCallback callback, Mobile from, int hue)
        {
            m_Timer = new SpinTimer(this, callback, from, hue);
            m_Timer.Start();

            foreach (var c in Components)
            {
                switch (c.ItemID)
                {
                    case 0x2E3D:
                    case 0x2E3F:
                        --c.ItemID;
                        break;
                }
            }
        }

        public override void OnComponentLoaded(AddonComponent c)
        {
            switch (c.ItemID)
            {
                case 0x2E3C:
                case 0x2E3E:
                    ++c.ItemID;
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
                    case 0x2E3C:
                    case 0x2E3E:
                        ++c.ItemID;
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
            private readonly ElvenSpinningWheelSouthAddon m_Wheel;

            public SpinTimer(ElvenSpinningWheelSouthAddon wheel, SpinCallback callback, Mobile from, int hue) : base(
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

<<<<<<< HEAD
    [Serializable(0, false)]
    public partial class ElvenSpinningwheelSouthDeed : BaseAddonDeed
=======
    [Serializable(0)]
    [TypeAlias("Server.Items.ElvenSpinningwheelSouthDeed")]
    public partial class ElvenSpinningWheelSouthDeed : BaseAddonDeed
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    {
        [Constructible]
        public ElvenSpinningWheelSouthDeed()
        {
        }

<<<<<<< HEAD
        public override BaseAddon Addon => new ElvenSpinningwheelSouthAddon();
        public override int LabelNumber => 1072878; // elven spining wheel (south
=======
        public override BaseAddon Addon => new ElvenSpinningWheelSouthAddon();
        public override int LabelNumber => 1072878; // elven spinning wheel (south)
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    }
}
