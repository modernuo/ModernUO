using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Misc
{
    public enum GiftResult
    {
        Backpack,
        BankBox
    }

    public static class GiftGiving
    {
        private static readonly List<GiftGiver> m_Givers = new();

        public static void Register(GiftGiver giver)
        {
            m_Givers.Add(giver);
        }

        [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
        public static void OnLogin(PlayerMobile pm)
        {
            if (pm.Account is not Account acct)
            {
                return;
            }

            var now = Core.Now;

            for (var i = 0; i < m_Givers.Count; ++i)
            {
                var giver = m_Givers[i];

                if (now < giver.Start || now >= giver.Finish)
                {
                    continue; // not in the correct time frame
                }

                if (acct.Created > giver.Start - giver.MinimumAge)
                {
                    continue; // newly created account
                }

                if (acct.LastLogin >= giver.Start)
                {
                    continue; // already got one
                }

                giver.DelayGiveGift(TimeSpan.FromSeconds(5.0), pm);
            }

            acct.LastLogin = now;
        }
    }

    public abstract class GiftGiver
    {
        public virtual TimeSpan MinimumAge => TimeSpan.FromDays(30.0);

        public abstract DateTime Start { get; }
        public abstract DateTime Finish { get; }
        public abstract void GiveGift(Mobile mob);

        public virtual void DelayGiveGift(TimeSpan delay, Mobile mob)
        {
            Timer.StartTimer(delay, () => GiveGift(mob));
        }

        public virtual GiftResult GiveGift(Mobile mob, Item item)
        {
            if (mob.PlaceInBackpack(item) && !StaminaSystem.IsOverloaded(mob))
            {
                return GiftResult.Backpack;
            }

            mob.BankBox.DropItem(item);
            return GiftResult.BankBox;
        }
    }
}
