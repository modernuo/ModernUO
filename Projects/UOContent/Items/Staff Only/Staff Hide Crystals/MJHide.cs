using System;

namespace Server.Items
{
    [Serializable(0)]
    public partial class MJHide : BaseStaffHide
    {
        private Timer _timer;

        public override string DefaultName => "Staff MJ Hide";

        public override bool CastHide => false;

        [Constructible]
        public MJHide() : base(2119)
        {
        }

        public override void HideEffects(Mobile from)
        {
            if (from.Hidden)
            {
                from.Hidden = false;
                from.Animate(1, 5, 1, true, false, 0);
                from.FixedParticles(0x3709, 1, 30, 9965, 5, 7, EffectLayer.Waist);
                from.FixedParticles(0x376A, 1, 30, 9502, 5, 3, EffectLayer.Waist);

                from.PlaySound(0x244);
            }
            else
            {
                _timer = new Countdown(from, this);
                _timer.Start();
            }
        }

        public class Countdown: Timer
        {
            private int _count;
            private readonly Mobile _mobile;
            private readonly MJHide _item;

            public Countdown(Mobile mobile, MJHide item): base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                _mobile = mobile;
                _item = item;
                _count = 2;
            }

            private static void Emote(Mobile from)
            {
                from.BoltEffect(0);
                from.Animate(22, 5, 1, true, false, 0);
            }

            protected override void OnTick()
            {
                switch (_count)
                {
                    case 2:
                        {
                            Emote(_mobile);
                            break;
                        }
                    case 1:
                        {
                            _mobile.Hidden = true;
                            break;
                        }
                    case 0:
                        {
                            Stop();
                            _item._timer = null;
                            break;
                        }
                }

                _count--;
            }
        }
    }
}
