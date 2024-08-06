using Server.Engines.Virtues;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class PricedResurrectGump : StaticGump<PricedResurrectGump>
{
    private readonly Mobile m_Healer;
    private readonly int m_Price;

    public PricedResurrectGump(Mobile owner, Mobile healer, int price)
        : base(150, 50)
    {
        m_Healer = healer;
        m_Price = price;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage(0);

        builder.AddImage(0, 0, 3600);

        builder.AddImageTiled(0, 14, 15, 200, 3603);
        builder.AddImageTiled(380, 14, 14, 200, 3605);

        builder.AddImage(0, 201, 3606);

        builder.AddImageTiled(15, 201, 370, 16, 3607);
        builder.AddImageTiled(15, 0, 370, 16, 3601);

        builder.AddImage(380, 0, 3602);

        builder.AddImage(380, 201, 3608);

        builder.AddImageTiled(15, 15, 365, 190, 2624);

        builder.AddRadio(30, 140, 9727, 9730, true, 1);
        builder.AddHtmlLocalized(65, 145, 300, 25, 1060015, 0x7FFF); // Grudgingly pay the money

        builder.AddRadio(30, 175, 9727, 9730, false, 0);
        builder.AddHtmlLocalized(65, 178, 300, 25, 1060016, 0x7FFF); // I'd rather stay dead, you scoundrel!!!

        // Wishing to rejoin the living, are you?  I can restore your body... for a price of course...
        builder.AddHtmlLocalized(30, 20, 360, 35, 1060017, 0x7FFF);

        // Do you accept the fee, which will be withdrawn from your bank?
        builder.AddHtmlLocalized(30, 105, 345, 40, 1060018, 0x5B2D);

        builder.AddImage(65, 72, 5605);

        builder.AddImageTiled(80, 90, 200, 1, 9107);
        builder.AddImageTiled(95, 92, 200, 1, 9157);

        builder.AddLabel(90, 70, 1645, m_Price.ToString());
        builder.AddHtmlLocalized(140, 70, 100, 25, 1023823, 0x7FFF); // gold coins

        builder.AddButton(290, 175, 247, 248, 2);

        builder.AddImageTiled(15, 14, 365, 1, 9107);
        builder.AddImageTiled(380, 14, 1, 190, 9105);
        builder.AddImageTiled(15, 205, 365, 1, 9107);
        builder.AddImageTiled(15, 14, 1, 190, 9105);
        builder.AddImageTiled(0, 0, 395, 1, 9157);
        builder.AddImageTiled(394, 0, 1, 217, 9155);
        builder.AddImageTiled(0, 216, 395, 1, 9157);
        builder.AddImageTiled(0, 0, 1, 217, 9155);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (info.ButtonID != 1 && info.ButtonID != 2)
        {
            return;
        }

        if (from.Map?.CanFit(from.Location, 16, false, false) != true)
        {
            from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            return;
        }

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
    }
}
