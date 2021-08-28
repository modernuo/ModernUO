using System;

namespace Server.Items
{
    [Serializable(0)]
    public partial class FlamestormHide : BaseStaffHide
    {
        public override string DefaultName => "Firestorm Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            if (from is not { Deleted: false })
            {
                return;
            }

            if (from.Hidden)
            {
                from.HueMod = 0;
                from.BodyMod = 15;
                from.Hue = 0;
                from.BodyValue = 15;
                from.Hidden = false;
                from.Animate(12, 10, 0, true, false, 0);
                from.PlaySound(273);
                Timer.StartTimer(TimeSpan.FromSeconds(1.5), () => AnimStop_Callback(from, false));
            }
            else
            {
                from.Animate(17, 4, 1, true, false, 0);
                Timer.StartTimer(TimeSpan.FromSeconds(0.75), () => CastStop_Callback(from));
            }

        }

        private void AnimStop_Callback(Mobile from, bool toHide)
        {
            from.HueMod = -1;
            from.BodyMod = -1;

            if (toHide)
            {
                from.Hidden = true;
            }
            else
            {
                from.Animate(17, 4, 1, false, false, 0);
            }

            OnEndHideEffects(from);
        }

        private void CastStop_Callback(Mobile from)
        {
            from.HueMod = 0;
            from.BodyValue = 15;
            from.Animate(12, 8, 1, false, false, 0);
            from.PlaySound(274);
            Timer.StartTimer(TimeSpan.FromSeconds(1.5), () => AnimStop_Callback(from, true));
        }

        [Constructible]
        public FlamestormHide() : base(1160)
        {
        }
    }
}
