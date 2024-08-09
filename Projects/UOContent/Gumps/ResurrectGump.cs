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

    public override bool Singleton => true;

    public static void TryGiveStatLoss(PlayerMobile player)
    {
        if (Core.AOS || player.ShortTermMurders < ShortMurdersForStatLoss)
        {
            return;
        }

        var loss = Math.Clamp((100.0 - (4.0 + player.ShortTermMurders / (double)ShortMurdersForStatLoss)) / 100.0, 0.85, 0.95); // 5 to 15% loss
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

    public ResurrectGump(Mobile healer, double hitsScalar)
        : this(healer, ResurrectMessage.Generic, false, hitsScalar)
    {
    }

    public ResurrectGump(
        Mobile healer, ResurrectMessage msg = ResurrectMessage.Generic,
        bool fromSacrifice = false, double hitsScalar = 0.0
    ) : base(100, 0)
    {
        _healer = healer;
        _fromSacrifice = fromSacrifice;
        _hitsScalar = hitsScalar;
        _resurrectMessage = msg;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
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

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID is not 1 and not 2)
        {
            return;
        }

        var from = state.Mobile;

        if (from.Map?.CanFit(from.Location, 16, false, false) != true)
        {
            from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            return;
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
