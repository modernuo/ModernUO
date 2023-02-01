using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class CreateGuildGump : Gump
    {
        public CreateGuildGump(PlayerMobile pm, string guildName = "Guild Name", string guildAbbrev = "") : base(10, 10)
        {
            pm.CloseGump<CreateGuildGump>();
            pm.CloseGump<BaseGuildGump>();

            AddPage(0);

            AddBackground(0, 0, 500, 300, 0x2422);
            AddHtmlLocalized(25, 20, 450, 25, 1062939, 0x0, true); // <center>GUILD MENU</center>

            // As you are not a member of any guild, you can create your own by providing a unique guild name and paying the standard guild registration fee.
            AddHtmlLocalized(25, 60, 450, 60, 1062940, 0x0);

            AddHtmlLocalized(25, 135, 120, 25, 1062941, 0x0); // Registration Fee:
            AddLabel(155, 135, 0x481, Guild.RegistrationFee.ToString());
            AddHtmlLocalized(25, 165, 120, 25, 1011140, 0x0); // Enter Guild Name:
            AddBackground(155, 160, 320, 26, 0xBB8);
            AddTextEntry(160, 163, 315, 21, 0x481, 5, guildName);
            AddHtmlLocalized(25, 191, 120, 26, 1063035, 0x0); // Abbreviation:
            AddBackground(155, 186, 320, 26, 0xBB8);
            AddTextEntry(160, 189, 315, 21, 0x481, 6, guildAbbrev);
            AddButton(415, 217, 0xF7, 0xF8, 1);
            AddButton(345, 217, 0xF2, 0xF1, 0);

            if (pm.AcceptGuildInvites)
            {
                AddButton(20, 260, 0xD2, 0xD3, 2);
            }
            else
            {
                AddButton(20, 260, 0xD3, 0xD2, 2);
            }

            AddHtmlLocalized(45, 260, 200, 30, 1062943, 0x0); // <i>Ignore Guild Invites</i>
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (sender.Mobile is not PlayerMobile { Guild: null } pm)
            {
                return; // Sanity
            }

            switch (info.ButtonID)
            {
                case 1:
                    {
                        var tName = info.GetTextEntry(5);
                        var tAbbrev = info.GetTextEntry(6);

                        var guildName = tName?.Text?.Trim() ?? "";
                        var guildAbbrev = tAbbrev?.Text?.Trim() ?? "";

                        guildName = Utility.FixHtml(guildName);
                        guildAbbrev = Utility.FixHtml(guildAbbrev);

                        if (guildName.Length <= 0)
                        {
                            pm.SendLocalizedMessage(1070884); // Guild name cannot be blank.
                        }
                        else if (guildAbbrev.Length <= 0)
                        {
                            pm.SendLocalizedMessage(1070885); // You must provide a guild abbreviation.
                        }
                        else if (guildName.Length > Guild.NameLimit)
                        {
                            // A guild name cannot be more than ~1_val~ characters in length.
                            pm.SendLocalizedMessage(1063036, Guild.NameLimit.ToString());
                        }
                        else if (guildAbbrev.Length > Guild.AbbrevLimit)
                        {
                            // An abbreviation cannot exceed ~1_val~ characters in length.
                            pm.SendLocalizedMessage(1063037, Guild.AbbrevLimit.ToString());
                        }
                        else if (BaseGuild.FindByAbbrev(guildAbbrev) != null || !BaseGuildGump.CheckProfanity(guildAbbrev))
                        {
                            pm.SendLocalizedMessage(501153); // That abbreviation is not available.
                        }
                        else if (BaseGuild.FindByName(guildName) != null || !BaseGuildGump.CheckProfanity(guildName))
                        {
                            pm.SendLocalizedMessage(1063000); // That guild name is not available.
                        }
                        else if (!Banker.Withdraw(pm, Guild.RegistrationFee))
                        {
                            // You do not possess the ~1_val~ gold piece fee required to create a guild.
                            pm.SendLocalizedMessage(1063001, Guild.RegistrationFee.ToString());
                        }
                        else
                        {
                            // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                            pm.SendLocalizedMessage(1060398, Guild.RegistrationFee.ToString());

                            pm.SendLocalizedMessage(1063238); // Your new guild has been founded.
                            pm.Guild = new Guild(pm, guildName, guildAbbrev);
                        }

                        break;
                    }
                case 2:
                    {
                        pm.AcceptGuildInvites = !pm.AcceptGuildInvites;

                        if (pm.AcceptGuildInvites)
                        {
                            pm.SendLocalizedMessage(1070699); // You are now accepting guild invitations.
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1070698); // You are now ignoring guild invitations.
                        }

                        break;
                    }
            }
        }
    }
}
