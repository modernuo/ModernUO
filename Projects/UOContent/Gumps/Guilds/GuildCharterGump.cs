using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildCharterGump : DynamicGump
    {
        private const string DefaultWebsite = "https://www.modernuo.com";
        private readonly Guild _guild;
        private readonly Mobile _mobile;

        public override bool Singleton => true;

        private GuildCharterGump(Mobile from, Guild guild) : base(20, 30)
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
            from.SendGump(new GuildCharterGump(from, guild));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 400, 5054);
            builder.AddBackground(10, 10, 530, 380, 3000);

            builder.AddButton(20, 360, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 360, 300, 35, 1011120); // Return to the main menu.

            string charter;

            if ((charter = _guild.Charter) == null || (charter = charter.Trim()).Length <= 0)
            {
                builder.AddHtmlLocalized(20, 20, 400, 35, 1013032); // No charter has been defined.
            }
            else
            {
                builder.AddHtml(20, 20, 510, 75, charter, background: true, scrollbar: true);
            }

            builder.AddButton(20, 200, 4005, 4007, 2);
            builder.AddHtmlLocalized(55, 200, 300, 20, 1011122); // Visit the guild website :

            string website;

            if ((website = _guild.Website) == null || (website = website.Trim()).Length <= 0)
            {
                website = DefaultWebsite;
            }

            builder.AddHtml(55, 220, 300, 20, website);
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (GuildGump.BadMember(_mobile, _guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 0:
                    {
                        return; // Close
                    }
                case 1:
                    {
                        break; // Return to main menu
                    }
                case 2:
                    {
                        string website;

                        if ((website = _guild.Website) == null || (website = website.Trim()).Length <= 0)
                        {
                            website = DefaultWebsite;
                        }

                        _mobile.LaunchBrowser(website);
                        break;
                    }
            }

            GuildGump.DisplayTo(_mobile, _guild);
        }
    }
}
