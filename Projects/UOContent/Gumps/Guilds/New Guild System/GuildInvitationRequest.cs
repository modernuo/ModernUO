using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class GuildInvitationRequest : BaseGuildGump
    {
        private readonly PlayerMobile _inviter;

        public GuildInvitationRequest(PlayerMobile pm, Guild g, PlayerMobile inviter) : base(pm, g)
        {
            _inviter = inviter;
        }

        protected override bool ShowTabStrip => false;

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 350, 170, 0x2422);
            // <center>You have been invited to join a guild! (Warning: Accepting will make you attackable!)</center>
            builder.AddHtmlLocalized(25, 20, 300, 45, 1062946, 0x0, true);
            builder.AddHtml(25, 75, 300, 25, $"<center>{guild.Name}</center>", background: true);
            builder.AddButton(265, 130, 0xF7, 0xF8, 1);
            builder.AddButton(195, 130, 0xF2, 0xF1, 0);
            builder.AddButton(20, 130, 0xD2, 0xD3, 2);
            builder.AddHtmlLocalized(45, 130, 150, 30, 1062943, 0x0); // <i>Ignore Guild Invites</i>
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (guild.Disbanded || player.Guild != null)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 0:
                    {
                        // ~1_val~ has declined your invitation to join ~2_val~.
                        _inviter.SendLocalizedMessage(1063250, $"{player.Name}\t{guild.Name}");
                        break;
                    }
                case 1:
                    {
                        guild.AddMember(player);
                        player.SendLocalizedMessage(1063056, guild.Name); // You have joined ~1_val~.
                        // ~1_val~ has accepted your invitation to join ~2_val~.
                        _inviter.SendLocalizedMessage(1063249, $"{player.Name}\t{guild.Name}");

                        break;
                    }
                case 2:
                    {
                        player.AcceptGuildInvites = false;
                        player.SendLocalizedMessage(1070698); // You are now ignoring guild invitations.

                        break;
                    }
            }
        }
    }
}
