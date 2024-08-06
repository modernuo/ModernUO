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

public class ResurrectGump : StaticGump<ResurrectGump>
{
    private readonly bool m_FromSacrifice;
    private readonly Mobile m_Healer;
    private readonly double m_HitsScalar;
    private readonly ResurrectMessage m_ResurrectMessage;

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
    ) : base(100, 0)
    {
        m_Healer = healer;
        m_FromSacrifice = fromSacrifice;
        m_HitsScalar = hitsScalar;
        m_ResurrectMessage = msg;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage(0);

        builder.AddBackground(0, 0, 400, 350, 2600);

        builder.AddHtmlLocalized(0, 20, 400, 35, 1011022); // <center>Resurrection</center>

        /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
         * CONTINUE - You chose to try to come back to life now.<br>
         * CANCEL - You prefer to remain a ghost for now.
         */
        builder.AddHtmlLocalized(50, 55, 300, 140, 1011023 + (int)m_ResurrectMessage, true, true);

        builder.AddButton(200, 227, 4005, 4007, 0);
        builder.AddHtmlLocalized(235, 230, 110, 35, 1011012); // CANCEL

        builder.AddButton(65, 227, 4005, 4007, 1);
        builder.AddHtmlLocalized(100, 230, 110, 35, 1011011); // CONTINUE
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

        if (from.Fame > 0)
        {
            var amount = from.Fame / 10;

            Titles.AwardFame(from, -amount, true);
        }

        if (!Core.AOS && player?.ShortTermMurders >= 5)
        {
            var loss = Math.Clamp((100.0 - (4.0 + player.ShortTermMurders / 5.0)) / 100.0, 0.85, 0.95); // 5 to 15% loss

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
