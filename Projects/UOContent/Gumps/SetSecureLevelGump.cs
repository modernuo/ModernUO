using Server.Guilds;
using Server.Multis;
using Server.Network;

namespace Server.Gumps
{
    public interface ISecurable
    {
        SecureLevel Level { get; set; }
    }

    public class SetSecureLevelGump : Gump
    {
        private readonly ISecurable m_Info;

        public SetSecureLevelGump(Mobile owner, ISecurable info, BaseHouse house) : base(50, 50)
        {
            m_Info = info;

            AddPage(0);

            var offset = Guild.NewGuildSystem ? 20 : 0;

            AddBackground(0, 0, 220, 160 + offset, 5054);

            AddImageTiled(10, 10, 200, 20, 5124);
            AddImageTiled(10, 40, 200, 20, 5124);
            AddImageTiled(10, 70, 200, 80 + offset, 5124);

            AddAlphaRegion(10, 10, 200, 140);

            AddHtmlLocalized(10, 10, 200, 20, 1061276, 32767); // <CENTER>SET ACCESS</CENTER>
            AddHtmlLocalized(10, 40, 100, 20, 1041474, 32767); // Owner:

            AddLabel(110, 40, 1152, owner == null ? "" : owner.Name);

            AddButton(10, 70, GetFirstID(SecureLevel.Owner), 4007, 1);
            AddHtmlLocalized(45, 70, 150, 20, 1061277, GetColor(SecureLevel.Owner)); // Owner Only

            AddButton(10, 90, GetFirstID(SecureLevel.CoOwners), 4007, 2);
            AddHtmlLocalized(45, 90, 150, 20, 1061278, GetColor(SecureLevel.CoOwners)); // Co-Owners

            AddButton(10, 110, GetFirstID(SecureLevel.Friends), 4007, 3);
            AddHtmlLocalized(45, 110, 150, 20, 1061279, GetColor(SecureLevel.Friends)); // Friends

            var houseOwner = house.Owner;
            if (Guild.NewGuildSystem && houseOwner?.Guild != null &&
                ((Guild)houseOwner.Guild).Leader == houseOwner
            ) // Only the actual House owner AND guild master can set guild secures
            {
                AddButton(10, 130, GetFirstID(SecureLevel.Guild), 4007, 5);
                AddHtmlLocalized(45, 130, 150, 20, 1063455, GetColor(SecureLevel.Guild)); // Guild Members
            }

            AddButton(10, 130 + offset, GetFirstID(SecureLevel.Anyone), 4007, 4);
            AddHtmlLocalized(45, 130 + offset, 150, 20, 1061626, GetColor(SecureLevel.Anyone)); // Anyone
        }

        public int GetColor(SecureLevel level) => m_Info.Level == level ? 0x7F18 : 0x7FFF;

        public int GetFirstID(SecureLevel level) => m_Info.Level == level ? 4006 : 4005;

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var level = info.ButtonID switch
            {
                1 => SecureLevel.Owner,
                2 => SecureLevel.CoOwners,
                3 => SecureLevel.Friends,
                4 => SecureLevel.Anyone,
                5 => SecureLevel.Guild,
                _ => m_Info.Level
            };

            if (m_Info.Level == level)
            {
                state.Mobile.SendLocalizedMessage(1061281); // Access level unchanged.
            }
            else
            {
                m_Info.Level = level;
                state.Mobile.SendLocalizedMessage(1061280); // New access level set.
            }
        }
    }
}
