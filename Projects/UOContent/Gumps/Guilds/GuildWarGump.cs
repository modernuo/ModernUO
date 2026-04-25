using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildWarGump : DynamicGump
    {
        private readonly Guild _guild;
        private readonly Mobile _mobile;

        public override bool Singleton => true;

        private GuildWarGump(Mobile from, Guild guild) : base(20, 30)
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
            from.SendGump(new GuildWarGump(from, guild));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 440, 5054);
            builder.AddBackground(10, 10, 530, 420, 3000);

            builder.AddHtmlLocalized(20, 10, 500, 35, 1011133); // <center>WARFARE STATUS</center>

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 300, 35, 1011120); // Return to the main menu.

            builder.AddPage(1);

            builder.AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, 2);
            builder.AddHtmlLocalized(410, 373, 100, 25, 1011066); // Next page

            builder.AddHtmlLocalized(20, 45, 400, 20, 1011134); // We are at war with:

            var enemies = _guild.Enemies;

            if (enemies.Count == 0)
            {
                builder.AddHtmlLocalized(20, 65, 400, 20, 1013033); // No current wars
            }
            else
            {
                for (var i = 0; i < enemies.Count; ++i)
                {
                    var g = enemies[i];

                    builder.AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }

            builder.AddPage(2);

            builder.AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, 3);
            builder.AddHtmlLocalized(410, 373, 100, 25, 1011066); // Next page

            builder.AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, 1);
            builder.AddHtmlLocalized(65, 373, 150, 25, 1011067); // Previous page

            builder.AddHtmlLocalized(20, 45, 400, 20, 1011136); // Guilds that we have declared war on:

            var declared = _guild.WarDeclarations;

            if (declared.Count == 0)
            {
                builder.AddHtmlLocalized(20, 65, 400, 20, 1018012); // No current invitations received for war.
            }
            else
            {
                for (var i = 0; i < declared.Count; ++i)
                {
                    var g = declared[i];

                    builder.AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }

            builder.AddPage(3);

            builder.AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, 2);
            builder.AddHtmlLocalized(65, 373, 150, 25, 1011067); // Previous page

            builder.AddHtmlLocalized(20, 45, 400, 20, 1011135); // Guilds that have declared war on us:

            var invites = _guild.WarInvitations;

            if (invites.Count == 0)
            {
                builder.AddHtmlLocalized(20, 65, 400, 20, 1013055); // No current war declarations
            }
            else
            {
                for (var i = 0; i < invites.Count; ++i)
                {
                    var g = invites[i];

                    builder.AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (GuildGump.BadMember(_mobile, _guild))
            {
                return;
            }

            if (info.ButtonID == 1)
            {
                GuildGump.DisplayTo(_mobile, _guild);
            }
        }
    }
}
