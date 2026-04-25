using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildWarAdminGump : DynamicGump
    {
        private readonly Guild _guild;
        private readonly Mobile _mobile;

        public override bool Singleton => true;

        private GuildWarAdminGump(Mobile from, Guild guild) : base(20, 30)
        {
            _mobile = from;
            _guild = guild;
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildWarAdminGump(from, guild));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 440, 5054);
            builder.AddBackground(10, 10, 530, 420, 3000);

            builder.AddHtmlLocalized(20, 10, 510, 35, 1011105); // <center>WAR FUNCTIONS</center>

            builder.AddButton(20, 40, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 40, 400, 30, 1011099); // Declare war through guild name search.

            var count = 0;

            if (_guild.Enemies.Count > 0)
            {
                builder.AddButton(20, 160 + count * 30, 4005, 4007, 2);
                builder.AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011103); // Declare peace.
            }
            else
            {
                builder.AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1013033); // No current wars
            }

            if (_guild.WarInvitations.Count > 0)
            {
                builder.AddButton(20, 160 + count * 30, 4005, 4007, 3);
                builder.AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011100); // Accept war invitations.

                builder.AddButton(20, 160 + count * 30, 4005, 4007, 4);
                builder.AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011101); // Reject war invitations.
            }
            else
            {
                builder.AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1018012); // No current invitations received for war.
            }

            if (_guild.WarDeclarations.Count > 0)
            {
                builder.AddButton(20, 160 + count * 30, 4005, 4007, 5);
                builder.AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011102); // Rescind your war declarations.
            }
            else
            {
                builder.AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1013055); // No current war declarations
            }

            builder.AddButton(20, 400, 4005, 4007, 6);
            builder.AddHtmlLocalized(55, 400, 400, 35, 1011104); // Return to the previous menu.
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (GuildGump.BadLeader(_mobile, _guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Declare war
                    {
                        _mobile.SendLocalizedMessage(1018001); // Declare war through search - Enter Guild Name:
                        _mobile.Prompt = new GuildDeclareWarPrompt(_mobile, _guild);
                        break;
                    }
                case 2: // Declare peace
                    {
                        GuildDeclarePeaceGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 3: // Accept war
                    {
                        GuildAcceptWarGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 4: // Reject war
                    {
                        GuildRejectWarGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 5: // Rescind declarations
                    {
                        GuildRescindDeclarationGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 6: // Return
                    {
                        GuildmasterGump.DisplayTo(_mobile, _guild);
                        break;
                    }
            }
        }
    }
}
