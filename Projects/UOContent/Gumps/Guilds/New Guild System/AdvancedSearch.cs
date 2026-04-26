using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public delegate void SearchSelectionCallback(GuildDisplayType display);

    public class GuildAdvancedSearchGump : BaseGuildGump
    {
        private readonly SearchSelectionCallback _callback;
        private readonly GuildDisplayType _display;

        public GuildAdvancedSearchGump(PlayerMobile pm, Guild g, GuildDisplayType display, SearchSelectionCallback callback)
            : base(pm, g)
        {
            _callback = callback;
            _display = display;
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(431, 43, 110, 26, 1062978, 0xF); // Diplomacy

            builder.AddHtmlLocalized(65, 80, 480, 26, 1063124, 0xF, true); // <i>Advanced Search Options</i>

            // Showing All Guilds/w/Relation/Waiting Relation
            builder.AddHtmlLocalized(
                65,
                110,
                480,
                26,
                1063136 + (int)_display,
                0xF
            );

            builder.AddGroup(1);
            builder.AddRadio(75, 140, 0xD2, 0xD3, false, 2);
            builder.AddHtmlLocalized(105, 140, 200, 26, 1063006, 0x0); // Show Guilds with Relationship
            builder.AddRadio(75, 170, 0xD2, 0xD3, false, 1);
            builder.AddHtmlLocalized(105, 170, 200, 26, 1063005, 0x0); // Show Guilds Awaiting Action
            builder.AddRadio(75, 200, 0xD2, 0xD3, false, 0);
            builder.AddHtmlLocalized(105, 200, 200, 26, 1063007, 0x0); // Show All Guilds

            builder.AddBackground(450, 370, 100, 26, 0x2486);
            builder.AddButton(455, 375, 0x845, 0x846, 5);
            builder.AddHtmlLocalized(480, 373, 60, 26, 1006044, 0x0); // OK
            builder.AddBackground(340, 370, 100, 26, 0x2486);
            builder.AddButton(345, 375, 0x845, 0x846, 0);
            builder.AddHtmlLocalized(370, 373, 60, 26, 1006045, 0x0); // Cancel
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            base.OnResponse(sender, info);

            if (sender.Mobile is not PlayerMobile pm || !IsMember(pm, guild))
            {
                return;
            }

            if (info.ButtonID == 5)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (info.IsSwitched(i))
                    {
                        var display = (GuildDisplayType)i;
                        _callback(display);
                        break;
                    }
                }
            }
        }
    }
}
