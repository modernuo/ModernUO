using System;
using Server.Mobiles;

namespace Server
{
    public static class CompassionVirtue
    {
        private const int LossAmount = 500;
        private static readonly TimeSpan LossDelay = TimeSpan.FromDays(7.0);

        public static void Initialize()
        {
            VirtueGump.Register(105, OnVirtueUsed);
        }

        public static void OnVirtueUsed(Mobile from)
        {
            from.SendLocalizedMessage(1053001); // This virtue is not activated through the virtue menu.
        }

        public static void CheckAtrophy(Mobile from)
        {
            if (from is not PlayerMobile pm)
            {
                return;
            }

            try
            {
                if (pm.LastCompassionLoss + LossDelay < Core.Now)
                {
                    VirtueHelper.Atrophy(from, VirtueName.Compassion, LossAmount);
                    // OSI has no cliloc message for losing compassion.  Weird.
                    pm.LastCompassionLoss = Core.Now;
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
