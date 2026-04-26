using System.Runtime.CompilerServices;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public abstract class BaseGuildGump : DynamicGump
    {
        public override bool Singleton => true;

        protected BaseGuildGump(PlayerMobile pm, Guild g, int x = 10, int y = 10) : base(x, y)
        {
            guild = g;
            player = pm;
        }

        protected Guild guild { get; }

        protected PlayerMobile player { get; }

        // Subclasses that draw their own background/layout (e.g. OtherGuildInfo,
        // WarDeclarationGump, GuildMemberInfoGump) can opt out of the standard
        // 600x440 frame and three-button tab strip drawn at the top of the gump.
        protected virtual bool ShowTabStrip => true;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            if (ShowTabStrip)
            {
                builder.AddBackground(0, 0, 600, 440, 0x24AE);
                builder.AddBackground(66, 40, 150, 26, 0x2486);
                builder.AddButton(71, 45, 0x845, 0x846, 1);
                builder.AddHtmlLocalized(96, 43, 110, 26, 1063014, 0x0); // My Guild
                builder.AddBackground(236, 40, 150, 26, 0x2486);
                builder.AddButton(241, 45, 0x845, 0x846, 2);
                builder.AddHtmlLocalized(266, 43, 110, 26, 1062974, 0x0); // Guild Roster
                builder.AddBackground(401, 40, 150, 26, 0x2486);
                builder.AddButton(406, 45, 0x845, 0x846, 3);
                builder.AddHtmlLocalized(431, 43, 110, 26, 1062978, 0x0); // Diplomacy
                builder.AddPage(1);
            }

            BuildContent(ref builder);
        }

        protected abstract void BuildContent(ref DynamicGumpBuilder builder);

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (sender.Mobile is not PlayerMobile pm)
            {
                return;
            }

            if (!IsMember(pm, guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    {
                        pm.SendGump(new GuildInfoGump(pm, guild));
                        break;
                    }
                case 2:
                    {
                        pm.SendGump(new GuildRosterGump(pm, guild));
                        break;
                    }
                case 3:
                    {
                        pm.SendGump(new GuildDiplomacyGump(pm, guild));
                        break;
                    }
            }
        }

        public static bool IsLeader(Mobile m, Guild g) =>
            !(m.Deleted || g.Disbanded || m is not PlayerMobile ||
              m.AccessLevel < AccessLevel.GameMaster && g.Leader != m);

        public static bool IsMember(Mobile m, Guild g) =>
            !(m.Deleted || g.Disbanded || m is not PlayerMobile ||
              m.AccessLevel < AccessLevel.GameMaster && !g.IsMember(m));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckProfanity(string s, int maxLength = 50) =>
            NameVerification.Validate(
                s,
                1,
                maxLength,
                true,
                true,
                false,
                0,
                ProfanityProtection.Exceptions,
                ProfanityProtection.Disallowed,
                ProfanityProtection.DisallowedSearchValues
            );

        protected static void AddHtmlText(
            ref DynamicGumpBuilder builder,
            int x,
            int y,
            int width,
            int height,
            TextDefinition text,
            bool back,
            bool scroll
        )
        {
            if (text?.Number > 0)
            {
                builder.AddHtmlLocalized(x, y, width, height, text.Number, back, scroll);
            }
            else if (text?.String != null)
            {
                builder.AddHtml(x, y, width, height, text.String, background: back, scrollbar: scroll);
            }
        }
    }
}
