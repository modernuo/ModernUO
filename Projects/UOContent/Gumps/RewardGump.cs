using System;
using Server.Network;

namespace Server.Gumps
{
    /*
     * A generic version of the EA Clean Up Britannia reward gump.
     */

    public interface IRewardEntry
    {
        int Price { get; }
        int ItemID { get; }
        int Hue { get; }
        int Tooltip { get; }
        TextDefinition Description { get; }
    }

    public delegate void RewardPickedHandler(Mobile from, int index);

    public class RewardGump : Gump
    {
        public RewardGump(TextDefinition title, IRewardEntry[] rewards, int points, RewardPickedHandler onPicked)
            : base(250, 50)
        {
            Title = title;
            Rewards = rewards;
            Points = points;
            OnPicked = onPicked;

            AddPage(0);

            AddImage(0, 0, 0x1F40);
            AddImageTiled(20, 37, 300, 308, 0x1F42);
            AddImage(20, 325, 0x1F43);
            AddImage(35, 8, 0x39);
            AddImageTiled(65, 8, 257, 10, 0x3A);
            AddImage(290, 8, 0x3B);
            AddImage(32, 33, 0x2635);
            AddImageTiled(70, 55, 230, 2, 0x23C5);

            if (Title.String != null)
            {
                AddHtml(70, 35, 270, 20, Title.String);
            }
            else if (Title.Number != 0)
            {
                AddHtmlLocalized(70, 35, 270, 20, Title.Number, 1);
            }

            AddHtmlLocalized(50, 65, 150, 20, 1072843, 1); // Your Reward Points:
            AddLabel(230, 65, 0x64, Points.ToString());
            AddImageTiled(35, 85, 270, 2, 0x23C5);
            AddHtmlLocalized(35, 90, 270, 20, 1072844, 1); // Please Choose a Reward:

            AddPage(1);

            var offset = 110;
            var page = 1;

            for (var i = 0; i < Rewards.Length; ++i)
            {
                var entry = Rewards[i];

                var bounds = ItemBounds.Table[entry.ItemID];
                var height = Math.Max(36, bounds.Height);

                if (offset + height > 320)
                {
                    AddHtmlLocalized(240, 335, 60, 20, 1072854, 1); // <div align=right>Next</div>
                    AddButton(300, 335, 0x15E1, 0x15E5, 51, GumpButtonType.Page, page + 1);

                    AddPage(++page);

                    AddButton(150, 335, 0x15E3, 0x15E7, 52, GumpButtonType.Page, page - 1);
                    AddHtmlLocalized(170, 335, 60, 20, 1074880, 1); // Previous

                    offset = 110;
                }

                var available = entry.Price <= Points;
                var half = offset + height / 2;

                if (available)
                {
                    AddButton(35, half - 6, 0x837, 0x838, 100 + i);
                }

                AddItem(
                    83 - bounds.Width / 2 - bounds.X,
                    half - bounds.Height / 2 - bounds.Y,
                    entry.ItemID,
                    available ? entry.Hue : 995
                );

                if (entry.Tooltip != 0)
                {
                    AddTooltip(entry.Tooltip);
                }

                AddLabel(133, half - 10, available ? 0x64 : 0x21, entry.Price.ToString());

                if (entry.Description != null)
                {
                    if (entry.Description.String != null)
                    {
                        AddHtml(190, offset, 114, height, entry.Description.String);
                    }
                    else if (entry.Description.Number != 0)
                    {
                        AddHtmlLocalized(190, offset, 114, height, entry.Description.Number, 1);
                    }
                }

                offset += height + 10;
            }
        }

        public TextDefinition Title { get; }

        public IRewardEntry[] Rewards { get; }

        public int Points { get; }

        public RewardPickedHandler OnPicked { get; }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var choice = info.ButtonID;

            if (choice == 0)
            {
                return; // Close
            }

            choice -= 100;

            if (choice >= 0 && choice < Rewards.Length)
            {
                var entry = Rewards[choice];

                if (entry.Price <= Points)
                {
                    sender.Mobile.SendGump(new RewardConfirmGump(this, choice, entry));
                }
            }
        }
    }

    public class RewardConfirmGump : Gump
    {
        private readonly int m_Index;
        private readonly RewardGump m_Parent;

        public RewardConfirmGump(RewardGump parent, int index, IRewardEntry entry)
            : base(120, 50)
        {
            m_Parent = parent;
            m_Index = index;

            Closable = false;

            AddPage(0);

            AddImageTiled(0, 0, 348, 262, 0xA8E);
            AddAlphaRegion(0, 0, 348, 262);
            AddImage(0, 15, 0x27A8);
            AddImageTiled(0, 30, 17, 200, 0x27A7);
            AddImage(0, 230, 0x27AA);
            AddImage(15, 0, 0x280C);
            AddImageTiled(30, 0, 300, 17, 0x280A);
            AddImage(315, 0, 0x280E);
            AddImage(15, 244, 0x280C);
            AddImageTiled(30, 244, 300, 17, 0x280A);
            AddImage(315, 244, 0x280E);
            AddImage(330, 15, 0x27A8);
            AddImageTiled(330, 30, 17, 200, 0x27A7);
            AddImage(330, 230, 0x27AA);
            AddImage(333, 2, 0x2716);
            AddImage(333, 248, 0x2716);
            AddImage(2, 248, 0x2716);
            AddImage(2, 2, 0x2716);

            AddItem(140, 120, entry.ItemID, entry.Hue);

            if (entry.Tooltip != 0)
            {
                AddTooltip(entry.Tooltip);
            }

            AddHtmlLocalized(25, 22, 200, 20, 1074974, 0x7D00); // Confirm Selection
            AddImage(25, 40, 0xBBF);
            AddHtmlLocalized(25, 55, 300, 120, 1074975, 0xFFFFFF); // Are you sure you wish to select this?
            AddRadio(25, 175, 0x25F8, 0x25FB, true, 1);
            AddRadio(25, 210, 0x25F8, 0x25FB, false, 0);
            AddHtmlLocalized(60, 180, 280, 20, 1074976, 0xFFFFFF); // Yes
            AddHtmlLocalized(60, 215, 280, 20, 1074977, 0xFFFFFF); // No
            AddButton(265, 220, 0xF7, 0xF8, 7);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 7 && info.IsSwitched(1))
            {
                m_Parent.OnPicked(sender.Mobile, m_Index);
            }
            else
            {
                sender.Mobile.SendGump(new RewardGump(m_Parent.Title, m_Parent.Rewards, m_Parent.Points, m_Parent.OnPicked));
            }
        }
    }
}
