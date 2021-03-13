using System;
using Server.Factions;
using Server.Guilds;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class GuildChangeTypeGump : Gump
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildChangeTypeGump(Mobile from, Guild guild) : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Draggable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 400, 5054);
            AddBackground(10, 10, 530, 380, 3000);

            AddHtmlLocalized(20, 15, 510, 30, 1013062); // <center>Change Guild Type Menu</center>

            AddHtmlLocalized(50, 50, 450, 30, 1013066); // Please select the type of guild you would like to change to

            AddButton(20, 100, 4005, 4007, 1);
            AddHtmlLocalized(85, 100, 300, 30, 1013063); // Standard guild

            AddButton(20, 150, 4005, 4007, 2);
            AddItem(50, 143, 7109);
            AddHtmlLocalized(85, 150, 300, 300, 1013064); // Order guild

            AddButton(20, 200, 4005, 4007, 3);
            AddItem(45, 200, 7107);
            AddHtmlLocalized(85, 200, 300, 300, 1013065); // Chaos guild

            AddButton(300, 360, 4005, 4007, 4);
            AddHtmlLocalized(335, 360, 150, 30, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (Guild.NewGuildSystem && !BaseGuildGump.IsLeader(m_Mobile, m_Guild) ||
                !Guild.NewGuildSystem && GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            var newType = info.ButtonID switch
            {
                1 => GuildType.Regular,
                2 => GuildType.Order,
                3 => GuildType.Chaos,
                _ => m_Guild.Type
            };

            if (m_Guild.Type != newType)
            {
                var pl = PlayerState.Find(m_Mobile);

                if (pl != null)
                {
                    m_Mobile.SendLocalizedMessage(1010405); // You cannot change guild types while in a Faction!
                }
                else if (m_Guild.TypeLastChange.AddDays(7) > Core.Now)
                {
                    m_Mobile.SendLocalizedMessage(1011142); // You have already changed your guild type recently.
                    // TODO: Clilocs 1011142-1011145 suggest a timer for pending changes
                }
                else
                {
                    m_Guild.Type = newType;
                    m_Guild.GuildMessage(1018022, true, newType.ToString()); // Guild Message: Your guild type has changed:
                }
            }

            if (Guild.NewGuildSystem)
            {
                if (m_Mobile is PlayerMobile mobile)
                {
                    mobile.SendGump(new GuildInfoGump(mobile, m_Guild));
                }

                return;
            }

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }
    }
}
