using System.Collections.Generic;
using Server.Network;

namespace Server.Gumps
{
    public interface IRewardOption
    {
        void GetOptions(RewardOptionList list);
        void OnOptionSelected(Mobile from, int choice);
    }

    public class RewardOptionGump : Gump
    {
        private readonly IRewardOption m_Option;
        private readonly RewardOptionList m_Options = new();

        public RewardOptionGump(IRewardOption option, int title = 0) : base(60, 36)
        {
            m_Option = option;

            m_Option?.GetOptions(m_Options);

            AddPage(0);

            AddBackground(0, 0, 273, 324, 0x13BE);
            AddImageTiled(10, 10, 253, 20, 0xA40);
            AddImageTiled(10, 40, 253, 244, 0xA40);
            AddImageTiled(10, 294, 253, 20, 0xA40);
            AddAlphaRegion(10, 10, 253, 304);

            AddButton(10, 294, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL

            if (title > 0)
            {
                AddHtmlLocalized(14, 12, 273, 20, title, 0x7FFF);
            }
            else
            {
                AddHtmlLocalized(14, 12, 273, 20, 1080392, 0x7FFF); // Select your choice from the menu below.
            }

            AddPage(1);

            for (var i = 0; i < m_Options.Count; i++)
            {
                AddButton(19, 49 + i * 24, 0x845, 0x846, m_Options[i].ID);
                AddHtmlLocalized(44, 47 + i * 24, 213, 20, m_Options[i].Cliloc, 0x7FFF);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Option != null && Contains(info.ButtonID))
            {
                m_Option.OnOptionSelected(sender.Mobile, info.ButtonID);
            }
        }

        private bool Contains(int chosen)
        {
            if (m_Options == null)
            {
                return false;
            }

            foreach (var option in m_Options)
            {
                if (option.ID == chosen)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class RewardOption
    {
        public RewardOption(int id, int cliloc)
        {
            ID = id;
            Cliloc = cliloc;
        }

        public int ID { get; }

        public int Cliloc { get; }
    }

    public class RewardOptionList : List<RewardOption>
    {
        public void Add(int id, int cliloc)
        {
            Add(new RewardOption(id, cliloc));
        }
    }
}
