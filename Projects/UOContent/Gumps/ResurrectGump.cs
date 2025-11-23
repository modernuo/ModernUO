using System;
using Server.Engines.Virtues;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public enum ResurrectMessage
{
    ChaosShrine = 0,
    VirtueShrine = 1,
    Healer = 2,
    Generic = 3
}

public class ResurrectGump : DynamicGump
{
    public const int ShortMurdersForStatLoss = 5;

    private readonly bool _fromSacrifice;
    private readonly Mobile _healer;
    private readonly double _hitsScalar;
    private readonly ResurrectMessage _resurrectMessage;
    private readonly int _price;

    public override bool Singleton => true;

    public static void TryGiveStatLoss(PlayerMobile player)
    {
        if (Core.AOS || player.ShortTermMurders < ShortMurdersForStatLoss)
        {
            return;
        }

        var loss = Math.Clamp(
            (100.0 - (4.0 + player.ShortTermMurders / (double)ShortMurdersForStatLoss)) / 100.0,
            0.85,
            0.95
        ); // 5 to 15% loss
        var lossStr = (int)(player.RawStr * loss);
        var lossInt = (int)(player.RawInt * loss);
        var lossDex = (int)(player.RawDex * loss);

        if (lossStr >= 10)
        {
            player.RawStr = lossStr;
        }

        if (lossInt >= 10)
        {
            player.RawInt = lossInt;
        }

        if (lossDex >= 10)
        {
            player.RawDex = lossDex;
        }

        for (var s = 0; s < player.Skills.Length; s++)
        {
            var skill = player.Skills[s];
            var skillLoss = (int)(skill.BaseFixedPoint * loss);
            if (skillLoss >= 350)
            {
                skill.BaseFixedPoint = skillLoss;
            }
        }
    }

    public ResurrectGump(Mobile healer, double hitsScalar) : this(healer, ResurrectMessage.Generic, false, hitsScalar)
    {
    }

    public ResurrectGump(Mobile healer, int price) : this(healer, ResurrectMessage.Generic, false, 0, price)
    {
    }

    public ResurrectGump(
        Mobile healer, ResurrectMessage msg = ResurrectMessage.Generic,
        bool fromSacrifice = false, double hitsScalar = 0.0, int price = 0
    ) : base(100, 0)
    {
        _healer = healer;
        _fromSacrifice = fromSacrifice;
        _hitsScalar = hitsScalar;
        _resurrectMessage = msg;
        _price = price;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_price > 0)
        {
            BuildPricedLayout(ref builder);
            return;
        }

        BuildDefaultLayout(ref builder);
    }

    private void BuildDefaultLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 400, 350, 2600);

        builder.AddHtmlLocalized(0, 20, 400, 35, 1011022); // <center>Resurrection</center>

        /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
         * CONTINUE - You chose to try to come back to life now.<br>
         * CANCEL - You prefer to remain a ghost for now.
         */
        builder.AddHtmlLocalized(50, 55, 300, 140, 1011023 + (int)_resurrectMessage, true, true);

        builder.AddButton(200, 227, 4005, 4007, 0);
        builder.AddHtmlLocalized(235, 230, 110, 35, 1011012); // CANCEL

        builder.AddButton(65, 227, 4005, 4007, 1);
        builder.AddHtmlLocalized(100, 230, 110, 35, 1011011); // CONTINUE
    }

    private void BuildPricedLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

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

        builder.AddLabel(90, 70, 1645, $"{_price}");
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
        if (_price > 0)
        {
            if (info.ButtonID != 2 || info.Switches[0] != 1)
            {
                return;
            }
        }
        else if (info.ButtonID != 1)
        {
            return;
        }

        var from = state.Mobile;

        if (from.Map?.CanFit(from.Location, 16, false, false) != true)
        {
            from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            return;
        }

        if (_price > 0)
        {
            if (Banker.Withdraw(from, _price))
            {
                // ~1_AMOUNT~ gold has been withdrawn from your bank to cover the price of the healing.
                from.SendLocalizedMessage(1060021, _price.ToString());

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

        from.PlaySound(0x214);
        from.FixedEffect(0x376A, 10, 16);

        from.Resurrect();

        if (_healer != null && from != _healer)
        {
            var level = VirtueSystem.GetLevel(_healer, VirtueName.Compassion);

            from.Hits = level switch
            {
                VirtueLevel.Seeker   => AOS.Scale(from.HitsMax, 20),
                VirtueLevel.Follower => AOS.Scale(from.HitsMax, 40),
                VirtueLevel.Knight   => AOS.Scale(from.HitsMax, 80),
                _                    => from.Hits
            };
        }

        if (from is PlayerMobile player)
        {
            if (_fromSacrifice)
            {
                player.Virtues.AvailableResurrects -= 1;

                var pack = player.Backpack;
                var corpse = player.Corpse;

                if (pack != null && corpse != null)
                {
                    for (var i = corpse.Items.Count - 1; i >= 0; --i)
                    {
                        var item = corpse.Items[i];

                        if (item.Layer != Layer.Hair && item.Layer != Layer.FacialHair && item.Movable)
                        {
                            pack.DropItem(item);
                        }
                    }
                }
            }

            TryGiveStatLoss(player);
        }

        if (from.Fame > 0)
        {
            var amount = from.Fame / 10;

            Titles.AwardFame(from, -amount, true);
        }

        if (from.Alive && _hitsScalar > 0)
        {
            from.Hits = (int)(from.HitsMax * _hitsScalar);
        }
    }
}
