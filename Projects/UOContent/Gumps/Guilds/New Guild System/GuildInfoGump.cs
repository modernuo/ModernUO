using System;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class GuildInfoGump : BaseGuildGump
    {
        private readonly bool _isResigning;

        public GuildInfoGump(PlayerMobile pm, Guild g, bool isResigning = false) : base(pm, g) => _isResigning = isResigning;

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            var isLeader = IsLeader(Player, Guild);

            builder.AddHtmlLocalized(96, 43, 110, 26, 1063014, 0xF); // My Guild

            builder.AddImageTiled(65, 80, 160, 26, 0xA40);
            builder.AddImageTiled(67, 82, 156, 22, 0xBBC);
            builder.AddHtmlLocalized(70, 83, 150, 20, 1062954, 0x0); // <i>Guild Name</i>
            builder.AddHtml(233, 84, 320, 26, Guild.Name);

            builder.AddImageTiled(65, 114, 160, 26, 0xA40);
            builder.AddImageTiled(67, 116, 156, 22, 0xBBC);
            builder.AddHtmlLocalized(70, 117, 150, 20, 1063025, 0x0); // <i>Alliance</i>

            if (Guild.Alliance?.IsMember(Guild) == true)
            {
                builder.AddHtml(233, 118, 320, 26, Guild.Alliance.Name);
                builder.AddButton(40, 120, 0x4B9, 0x4BA, 6); // Alliance Roster
            }

            if (Guild.OrderChaos && isLeader)
            {
                builder.AddButton(40, 154, 0x4B9, 0x4BA, 100); // Guild Faction
            }

            builder.AddImageTiled(65, 148, 160, 26, 0xA40);
            builder.AddImageTiled(67, 150, 156, 22, 0xBBC);
            builder.AddHtmlLocalized(70, 151, 150, 20, 1063084, 0x0); // <i>Guild Faction</i>

            GuildType gt;
            Faction f;

            if ((gt = Guild.Type) != GuildType.Regular)
            {
                builder.AddHtml(233, 152, 320, 26, $"{gt}");
            }
            else if ((f = Faction.Find(Guild.Leader)) != null)
            {
                builder.AddHtml(233, 152, 320, 26, $"{f}");
            }

            builder.AddImageTiled(65, 196, 480, 4, 0x238D);

            var s = Guild.Charter.DefaultIfNullOrEmpty("The guild leader has not yet set the guild charter.");

            builder.AddHtml(65, 216, 480, 80, s, background: true, scrollbar: true);
            if (isLeader)
            {
                builder.AddButton(40, 251, 0x4B9, 0x4BA, 4); // Charter Edit button
            }

            s = Guild.Website.DefaultIfNullOrEmpty("Guild website not yet set.");

            builder.AddHtml(65, 306, 480, 30, s, background: true);
            if (isLeader)
            {
                builder.AddButton(40, 313, 0x4B9, 0x4BA, 5); // Website Edit button
            }

            builder.AddCheckbox(65, 370, 0xD2, 0xD3, Player.DisplayGuildTitle, 0);
            builder.AddHtmlLocalized(95, 370, 150, 26, 1063085, 0x0); // Show Guild Title
            builder.AddBackground(450, 370, 100, 26, 0x2486);

            builder.AddButton(455, 375, 0x845, 0x846, 7);
            builder.AddHtmlLocalized(480, 373, 60, 26, 3006115, _isResigning ? 0x5000 : 0); // Resign
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            base.OnResponse(sender, info);

            var pm = (PlayerMobile)sender.Mobile;

            if (!IsMember(pm, Guild))
            {
                return;
            }

            pm.DisplayGuildTitle = info.IsSwitched(0);

            switch (info.ButtonID)
            {
                // 1-3 handled by base.OnResponse
                case 4:
                    {
                        if (IsLeader(pm, Guild))
                        {
                            pm.SendLocalizedMessage(1013071); // Enter the new guild charter (50 characters max):

                            // Have the same callback handle both canceling and deletion cause the 2nd callback would
                            // just get a text of ""
                            pm.BeginPrompt(SetCharter_Callback, true);
                        }

                        break;
                    }
                case 5:
                    {
                        if (IsLeader(pm, Guild))
                        {
                            pm.SendLocalizedMessage(1013072); // Enter the new website for the guild (50 characters max):
                            pm.BeginPrompt(SetWebsite_Callback, true);
                        }

                        break;
                    }
                case 6:
                    {
                        // Alliance Roster
                        if (Guild.Alliance?.IsMember(Guild) == true)
                        {
                            pm.SendGump(new AllianceInfo.AllianceRosterGump(pm, Guild, Guild.Alliance));
                        }

                        break;
                    }
                case 7:
                    {
                        // Resign
                        if (!_isResigning)
                        {
                            pm.SendLocalizedMessage(1063332); // Are you sure you wish to resign from your guild?
                            pm.SendGump(new GuildInfoGump(pm, Guild, true));
                        }
                        else
                        {
                            Guild.RemoveMember(pm, 1063411); // You resign from your guild.
                        }

                        break;
                    }
                case 100: // Custom code to support Order/Chaos in the new guild system
                    {
                        // Guild Faction
                        if (Guild.OrderChaos && IsLeader(pm, Guild))
                        {
                            GuildChangeTypeGump.DisplayTo(pm, Guild);
                        }

                        break;
                    }
            }
        }

        public void SetCharter_Callback(Mobile from, string text)
        {
            if (!IsLeader(from, Guild))
            {
                return;
            }

            var charter = text.AsSpan().Trim().FixHtml();

            if (charter.Length > 50)
            {
                from.SendLocalizedMessage(1070774, "50"); // Your guild charter cannot exceed ~1_val~ characters.
            }
            else
            {
                Guild.Charter = charter;
                from.SendLocalizedMessage(1070775); // You submit a new guild charter.
            }
        }

        public void SetWebsite_Callback(Mobile from, string text)
        {
            if (!IsLeader(from, Guild))
            {
                return;
            }

            var site = text.AsSpan().Trim().FixHtml();

            if (site.Length > 50)
            {
                from.SendLocalizedMessage(1070777, "50"); // Your guild website cannot exceed ~1_val~ characters.
            }
            else
            {
                Guild.Website = site;
                from.SendLocalizedMessage(1070778); // You submit a new guild website.
            }
        }
    }
}
