using System.Collections.Generic;
using Server.Engines.Virtues;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public enum ResurrectMessage
    {
        ChaosShrine = 0,
        VirtueShrine = 1,
        Healer = 2,
        Generic = 3
    }

    public class ResurrectGump : Gump
    {
        private readonly bool m_FromSacrifice;
        private readonly Mobile m_Healer;
        private readonly double m_HitsScalar;
        private readonly int m_Price;

        public ResurrectGump(Mobile owner, double hitsScalar)
            : this(owner, owner, ResurrectMessage.Generic, false, hitsScalar)
        {
        }

        public ResurrectGump(Mobile owner, ResurrectMessage msg) : this(owner, owner, msg)
        {
        }

        public ResurrectGump(Mobile owner, bool fromSacrifice = false)
            : this(owner, owner, ResurrectMessage.Generic, fromSacrifice)
        {
        }

        public ResurrectGump(
            Mobile owner, Mobile healer, ResurrectMessage msg = ResurrectMessage.Generic,
            bool fromSacrifice = false, double hitsScalar = 0.0
        )
            : base(100, 0)
        {
            m_Healer = healer;
            m_FromSacrifice = fromSacrifice;
            m_HitsScalar = hitsScalar;

            AddPage(0);

            AddBackground(0, 0, 400, 350, 2600);

            AddHtmlLocalized(0, 20, 400, 35, 1011022); // <center>Resurrection</center>

            /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
             * CONTINUE - You chose to try to come back to life now.<br>
             * CANCEL - You prefer to remain a ghost for now.
             */
            AddHtmlLocalized(50, 55, 300, 140, 1011023 + (int)msg, true, true);

            AddButton(200, 227, 4005, 4007, 0);
            AddHtmlLocalized(235, 230, 110, 35, 1011012); // CANCEL

            AddButton(65, 227, 4005, 4007, 1);
            AddHtmlLocalized(100, 230, 110, 35, 1011011); // CONTINUE
        }

        public ResurrectGump(Mobile owner, Mobile healer, int price)
            : base(150, 50)
        {
            m_Healer = healer;
            m_Price = price;

            Closable = false;

            AddPage(0);

            AddImage(0, 0, 3600);

            AddImageTiled(0, 14, 15, 200, 3603);
            AddImageTiled(380, 14, 14, 200, 3605);

            AddImage(0, 201, 3606);

            AddImageTiled(15, 201, 370, 16, 3607);
            AddImageTiled(15, 0, 370, 16, 3601);

            AddImage(380, 0, 3602);

            AddImage(380, 201, 3608);

            AddImageTiled(15, 15, 365, 190, 2624);

            AddRadio(30, 140, 9727, 9730, true, 1);
            AddHtmlLocalized(65, 145, 300, 25, 1060015, 0x7FFF); // Grudgingly pay the money

            AddRadio(30, 175, 9727, 9730, false, 0);
            AddHtmlLocalized(65, 178, 300, 25, 1060016, 0x7FFF); // I'd rather stay dead, you scoundrel!!!

            // Wishing to rejoin the living, are you?  I can restore your body... for a price of course...
            AddHtmlLocalized(30, 20, 360, 35, 1060017, 0x7FFF);

            // Do you accept the fee, which will be withdrawn from your bank?
            AddHtmlLocalized(30, 105, 345, 40, 1060018, 0x5B2D);

            AddImage(65, 72, 5605);

            AddImageTiled(80, 90, 200, 1, 9107);
            AddImageTiled(95, 92, 200, 1, 9157);

            AddLabel(90, 70, 1645, price.ToString());
            AddHtmlLocalized(140, 70, 100, 25, 1023823, 0x7FFF); // gold coins

            AddButton(290, 175, 247, 248, 2);

            AddImageTiled(15, 14, 365, 1, 9107);
            AddImageTiled(380, 14, 1, 190, 9105);
            AddImageTiled(15, 205, 365, 1, 9107);
            AddImageTiled(15, 14, 1, 190, 9105);
            AddImageTiled(0, 0, 395, 1, 9157);
            AddImageTiled(394, 0, 1, 217, 9155);
            AddImageTiled(0, 216, 395, 1, 9157);
            AddImageTiled(0, 0, 1, 217, 9155);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = state.Mobile;

            from.CloseGump<ResurrectGump>();

            if (info.ButtonID != 1 && info.ButtonID != 2)
            {
                return;
            }

            if (from.Map?.CanFit(from.Location, 16, false, false) != true)
            {
                from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                return;
            }

            if (m_Price > 0)
            {
                if (info.IsSwitched(1))
                {
                    if (Banker.Withdraw(from, m_Price))
                    {
                        // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                        from.SendLocalizedMessage(1060398, m_Price.ToString());

                        // You have ~1_AMOUNT~ gold in cash remaining in your bank box.
                        from.SendLocalizedMessage(1060022, Banker.GetBalance(from).ToString());
                    }
                    else
                    {
                        // Unfortunately, you do not have enough cash in your bank to cover the cost of the healing.
                        from.SendLocalizedMessage(1060020);
                        return;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1060019); // You decide against paying the healer, and thus remain dead.
                    return;
                }
            }

            from.PlaySound(0x214);
            from.FixedEffect(0x376A, 10, 16);

            from.Resurrect();

            if (m_Healer != null && from != m_Healer)
            {
                var level = VirtueSystem.GetLevel(m_Healer, VirtueName.Compassion);

                from.Hits = level switch
                {
                    VirtueLevel.Seeker   => AOS.Scale(from.HitsMax, 20),
                    VirtueLevel.Follower => AOS.Scale(from.HitsMax, 40),
                    VirtueLevel.Knight   => AOS.Scale(from.HitsMax, 80),
                    _                    => from.Hits
                };
            }

            var player = from as PlayerMobile;

            if (m_FromSacrifice && player != null)
            {
                player.Virtues.AvailableResurrects -= 1;

                var pack = player.Backpack;
                var corpse = player.Corpse;

                if (pack != null && corpse != null)
                {
                    var items = new List<Item>(corpse.Items);

                    for (var i = 0; i < items.Count; ++i)
                    {
                        var item = items[i];

                        if (item.Layer != Layer.Hair && item.Layer != Layer.FacialHair && item.Movable)
                        {
                            pack.DropItem(item);
                        }
                    }
                }
            }

            if (from.Fame > 0)
            {
                var amount = from.Fame / 10;

                Titles.AwardFame(from, -amount, true);
            }

            if (!Core.AOS && player?.ShortTermMurders >= 5)
            {
                var loss = (100.0 - (4.0 + player.ShortTermMurders / 5.0)) / 100.0; // 5 to 15% loss

                if (loss < 0.85)
                {
                    loss = 0.85;
                }
                else if (loss > 0.95)
                {
                    loss = 0.95;
                }

                if (from.RawStr * loss > 10)
                {
                    from.RawStr = (int)(from.RawStr * loss);
                }

                if (from.RawInt * loss > 10)
                {
                    from.RawInt = (int)(from.RawInt * loss);
                }

                if (from.RawDex * loss > 10)
                {
                    from.RawDex = (int)(from.RawDex * loss);
                }

                for (var s = 0; s < from.Skills.Length; s++)
                {
                    if (from.Skills[s].Base * loss > 35)
                    {
                        from.Skills[s].Base *= loss;
                    }
                }
            }

            if (from.Alive && m_HitsScalar > 0)
            {
                from.Hits = (int)(from.HitsMax * m_HitsScalar);
            }
        }
    }
}
