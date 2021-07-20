using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public delegate void SearchSelectionCallback(GuildDisplayType display);

    public class GuildAdvancedSearchGump : BaseGuildGump
    {
        private readonly SearchSelectionCallback m_Callback;
        private readonly GuildDisplayType m_Display;

        public GuildAdvancedSearchGump(PlayerMobile pm, Guild g, GuildDisplayType display, SearchSelectionCallback callback)
            : base(pm, g)
        {
            m_Callback = callback;
            m_Display = display;
            PopulateGump();
        }

        public override void PopulateGump()
        {
            base.PopulateGump();

            AddHtmlLocalized(431, 43, 110, 26, 1062978, 0xF); // Diplomacy

            AddHtmlLocalized(65, 80, 480, 26, 1063124, 0xF, true); // <i>Advanced Search Options</i>

            AddHtmlLocalized(
                65,
                110,
                480,
                26,
                1063136 + (int)m_Display,
                0xF
            ); // Showing All Guilds/w/Relation/Waiting Relation

            AddGroup(1);
            AddRadio(75, 140, 0xD2, 0xD3, false, 2);
            AddHtmlLocalized(105, 140, 200, 26, 1063006, 0x0); // Show Guilds with Relationship
            AddRadio(75, 170, 0xD2, 0xD3, false, 1);
            AddHtmlLocalized(105, 170, 200, 26, 1063005, 0x0); // Show Guilds Awaiting Action
            AddRadio(75, 200, 0xD2, 0xD3, false, 0);
            AddHtmlLocalized(105, 200, 200, 26, 1063007, 0x0); // Show All Guilds

            AddBackground(450, 370, 100, 26, 0x2486);
            AddButton(455, 375, 0x845, 0x846, 5);
            AddHtmlLocalized(480, 373, 60, 26, 1006044, 0x0); // OK
            AddBackground(340, 370, 100, 26, 0x2486);
            AddButton(345, 375, 0x845, 0x846, 0);
            AddHtmlLocalized(370, 373, 60, 26, 1006045, 0x0); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
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
                        m_Callback(display);
                        break;
                    }
                }
            }
        }
    }
}
