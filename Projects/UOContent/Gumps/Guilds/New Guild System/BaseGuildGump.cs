using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public abstract class BaseGuildGump : Gump
    {
        public BaseGuildGump(PlayerMobile pm, Guild g, int x = 10, int y = 10) : base(x, y)
        {
            guild = g;
            player = pm;

            pm.CloseGump<BaseGuildGump>();
        }

        protected Guild guild { get; }

        protected PlayerMobile player { get; }

        // There's prolly a way to have all the vars set of inherited classes before something is called in the Ctor... but... I can't think of it right now, and I can't use Timer.DelayCall here :<

        public virtual void PopulateGump()
        {
            AddPage(0);

            AddBackground(0, 0, 600, 440, 0x24AE);
            AddBackground(66, 40, 150, 26, 0x2486);
            AddButton(71, 45, 0x845, 0x846, 1);
            AddHtmlLocalized(96, 43, 110, 26, 1063014, 0x0); // My Guild
            AddBackground(236, 40, 150, 26, 0x2486);
            AddButton(241, 45, 0x845, 0x846, 2);
            AddHtmlLocalized(266, 43, 110, 26, 1062974, 0x0); // Guild Roster
            AddBackground(401, 40, 150, 26, 0x2486);
            AddButton(406, 45, 0x845, 0x846, 3);
            AddHtmlLocalized(431, 43, 110, 26, 1062978, 0x0); // Diplomacy
            AddPage(1);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
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

        public static bool CheckProfanity(string s, int maxLength = 50)
        {
            // return NameVerification.Validate( s, 1, 50, true, true, false, int.MaxValue, ProfanityProtection.Exceptions, ProfanityProtection.Disallowed, ProfanityProtection.StartDisallowed );	//What am I doing wrong, this still allows chars like the <3 symbol... 3 AM.  someone change this to use this

            // With testing on OSI, Guild stuff seems to follow a 'simpler' method of profanity protection
            if (s.Length < 1 || s.Length > maxLength)
            {
                return false;
            }

            var exceptions = ProfanityProtection.Exceptions;

            s = s.ToLower();

            for (var i = 0; i < s.Length; ++i)
            {
                var c = s[i];

                if (c is < 'a' or > 'z' && c is < '0' or > '9')
                {
                    var except = false;

                    for (var j = 0; !except && j < exceptions.Length; j++)
                    {
                        if (c == exceptions[j])
                        {
                            except = true;
                        }
                    }

                    if (!except)
                    {
                        return false;
                    }
                }
            }

            var disallowed = ProfanityProtection.Disallowed;

            for (var i = 0; i < disallowed.Length; i++)
            {
                if (s.IndexOfOrdinal(disallowed[i]) != -1)
                {
                    return false;
                }
            }

            return true;
        }

        public void AddHtmlText(int x, int y, int width, int height, TextDefinition text, bool back, bool scroll)
        {
            if (text?.Number > 0)
            {
                AddHtmlLocalized(x, y, width, height, text.Number, back, scroll);
            }
            else if (text?.String != null)
            {
                AddHtml(x, y, width, height, text.String, back, scroll);
            }
        }

        public static string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
    }
}
