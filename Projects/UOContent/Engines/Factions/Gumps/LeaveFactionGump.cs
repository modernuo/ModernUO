using System;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
    public class LeaveFactionGump : FactionGump
    {
        private readonly PlayerMobile m_From;
        private Faction m_Faction;

        public LeaveFactionGump(PlayerMobile from, Faction faction) : base(20, 30)
        {
            m_From = from;
            m_Faction = faction;

            AddBackground(0, 0, 270, 120, 5054);
            AddBackground(10, 10, 250, 100, 3000);

            if (from.Guild is Guild guild && guild.Leader == from)
            {
                AddHtmlLocalized(
                    20,
                    15,
                    230,
                    60,
                    1018057, // Are you sure you want your entire guild to leave this faction?
                    true,
                    true
                );
            }
            else
            {
                // Are you sure you want to leave this faction?s
                AddHtmlLocalized(20, 15, 230, 60, 1018063, true, true);
            }

            AddHtmlLocalized(55, 80, 75, 20, 1011011); // CONTINUE
            AddButton(20, 80, 4005, 4007, 1);

            AddHtmlLocalized(170, 80, 75, 20, 1011012); // CANCEL
            AddButton(135, 80, 4005, 4007, 2);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 1: // continue
                    {
                        if (m_From.Guild is not Guild guild)
                        {
                            var pl = PlayerState.Find(m_From);

                            if (pl != null)
                            {
                                pl.Leaving = Core.Now;

                                if (TimeSpan.FromDays(3.0) == Faction.LeavePeriod)
                                {
                                    m_From.SendLocalizedMessage(1005065); // You will be removed from the faction in 3 days
                                }
                                else
                                {
                                    m_From.SendMessage(
                                        $"You will be removed from the faction in {Faction.LeavePeriod.TotalDays} days."
                                    );
                                }
                            }
                        }
                        else if (guild.Leader != m_From)
                        {
                            // You cannot quit the faction because you are not the guild master
                            m_From.SendLocalizedMessage(1005061);
                        }
                        else
                        {
                            m_From.SendLocalizedMessage(1042285); // Your guild is now quitting the faction.

                            for (var i = 0; i < guild.Members.Count; ++i)
                            {
                                var mob = guild.Members[i];
                                var pl = PlayerState.Find(mob);

                                if (pl != null)
                                {
                                    pl.Leaving = Core.Now;

                                    if (TimeSpan.FromDays(3.0) == Faction.LeavePeriod)
                                    {
                                        // Your guild will quit the faction in 3 days
                                        mob.SendLocalizedMessage(1005060);
                                    }
                                    else
                                    {
                                        mob.SendMessage(
                                            $"Your guild will quit the faction in {Faction.LeavePeriod.TotalDays} days."
                                        );
                                    }
                                }
                            }
                        }

                        break;
                    }
                case 2: // cancel
                    {
                        m_From.SendLocalizedMessage(500737); // Canceled resignation.
                        break;
                    }
            }
        }
    }
}
