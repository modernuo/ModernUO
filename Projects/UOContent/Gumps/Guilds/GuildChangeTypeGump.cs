using Server.Factions;
using Server.Guilds;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class GuildChangeTypeGump : DynamicGump
    {
        private readonly Guild _guild;
        private readonly Mobile _mobile;

        public override bool Singleton => true;

        private GuildChangeTypeGump(Mobile from, Guild guild) : base(20, 30)
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
            from.SendGump(new GuildChangeTypeGump(from, guild));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 400, 5054);
            builder.AddBackground(10, 10, 530, 380, 3000);

            builder.AddHtmlLocalized(20, 15, 510, 30, 1013062); // <center>Change Guild Type Menu</center>

            builder.AddHtmlLocalized(50, 50, 450, 30, 1013066); // Please select the type of guild you would like to change to

            builder.AddButton(20, 100, 4005, 4007, 1);
            builder.AddHtmlLocalized(85, 100, 300, 30, 1013063); // Standard guild

            builder.AddButton(20, 150, 4005, 4007, 2);
            builder.AddItem(50, 143, 7109);
            builder.AddHtmlLocalized(85, 150, 300, 300, 1013064); // Order guild

            builder.AddButton(20, 200, 4005, 4007, 3);
            builder.AddItem(45, 200, 7107);
            builder.AddHtmlLocalized(85, 200, 300, 300, 1013065); // Chaos guild

            builder.AddButton(300, 360, 4005, 4007, 4);
            builder.AddHtmlLocalized(335, 360, 150, 30, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (Guild.NewGuildSystem && !BaseGuildGump.IsLeader(_mobile, _guild) ||
                !Guild.NewGuildSystem && GuildGump.BadLeader(_mobile, _guild))
            {
                return;
            }

            var newType = info.ButtonID switch
            {
                1 => GuildType.Regular,
                2 => GuildType.Order,
                3 => GuildType.Chaos,
                _ => _guild.Type
            };

            if (_guild.Type != newType)
            {
                var pl = PlayerState.Find(_mobile);

                if (pl != null)
                {
                    _mobile.SendLocalizedMessage(1010405); // You cannot change guild types while in a Faction!
                }
                else if (_guild.TypeLastChange.AddDays(7) > Core.Now)
                {
                    _mobile.SendLocalizedMessage(1011142); // You have already changed your guild type recently.
                    // TODO: Clilocs 1011142-1011145 suggest a timer for pending changes
                }
                else
                {
                    _guild.Type = newType;
                    _guild.GuildMessage(1018022, true, newType.ToString()); // Guild Message: Your guild type has changed:
                }
            }

            if (Guild.NewGuildSystem)
            {
                if (_mobile is PlayerMobile mobile)
                {
                    mobile.SendGump(new GuildInfoGump(mobile, _guild));
                }

                return;
            }

            GuildmasterGump.DisplayTo(_mobile, _guild);
        }
    }
}
