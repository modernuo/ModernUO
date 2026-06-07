using System;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildCharterGump : DynamicGump
    {
        private const string DefaultWebsite = "https://www.modernuo.com";
        private readonly Guild _guild;

        public override bool Singleton => true;

        private GuildCharterGump(Guild guild) : base(20, 30) => _guild = guild;

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildCharterGump(guild));
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

            var website = _guild.Website != null ? _guild.Website.AsSpan().Trim() : "";
            if (website.Length <= 0)
            {
                website = DefaultWebsite;
            }

            builder.AddHtml(55, 220, 300, 20, website);
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;
            if (info.ButtonID == 0 || GuildGump.BadMember(from, _guild))
            {
                return;
            }

            if (info.ButtonID == 2)
            {
                var website = _guild.Website != null ? _guild.Website.AsSpan().Trim() : "";
                if (website.Length <= 0)
                {
                    website = DefaultWebsite;
                }

                from.LaunchBrowser(website);
            }

            GuildGump.DisplayTo(from, _guild);
        }
    }
}
