using System;
using Server.Network;

namespace Server.Gumps;

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

public class RewardGump : DynamicGump
{
    private readonly TextDefinition _title;
    private readonly IRewardEntry[] _rewards;
    private readonly int _points;
    private readonly RewardPickedHandler _onPicked;

    public override bool Singleton => true;

    public TextDefinition Title => _title;
    public IRewardEntry[] Rewards => _rewards;
    public int Points => _points;
    public RewardPickedHandler OnPicked => _onPicked;

    private RewardGump(TextDefinition title, IRewardEntry[] rewards, int points, RewardPickedHandler onPicked)
        : base(250, 50)
    {
        _title = title;
        _rewards = rewards;
        _points = points;
        _onPicked = onPicked;
    }

    public static void DisplayTo(Mobile from, TextDefinition title, IRewardEntry[] rewards, int points, RewardPickedHandler onPicked)
    {
        if (from?.NetState != null && rewards != null && rewards.Length != 0 && onPicked != null)
        {
            from.SendGump(new RewardGump(title, rewards, points, onPicked));
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(0, 0, 0x1F40);
        builder.AddImageTiled(20, 37, 300, 308, 0x1F42);
        builder.AddImage(20, 325, 0x1F43);
        builder.AddImage(35, 8, 0x39);
        builder.AddImageTiled(65, 8, 257, 10, 0x3A);
        builder.AddImage(290, 8, 0x3B);
        builder.AddImage(32, 33, 0x2635);
        builder.AddImageTiled(70, 55, 230, 2, 0x23C5);

        _title.AddHtmlText(ref builder, 70, 35, 270, 20, numberColor: 1);

        builder.AddHtmlLocalized(50, 65, 150, 20, 1072843, 1); // Your Reward Points:
        builder.AddLabel(230, 65, 0x64, $"{_points}");
        builder.AddImageTiled(35, 85, 270, 2, 0x23C5);
        builder.AddHtmlLocalized(35, 90, 270, 20, 1072844, 1); // Please Choose a Reward:

        builder.AddPage(1);

        var offset = 110;
        var page = 1;

        for (var i = 0; i < _rewards.Length; ++i)
        {
            var entry = _rewards[i];

            var bounds = ItemBounds.Bounds[entry.ItemID];
            var height = Math.Max(36, bounds.Height);

            if (offset + height > 320)
            {
                builder.AddHtmlLocalized(240, 335, 60, 20, 1072854, 1); // <div align=right>Next</div>
                builder.AddButton(300, 335, 0x15E1, 0x15E5, 51, GumpButtonType.Page, page + 1);

                builder.AddPage(++page);

                builder.AddButton(150, 335, 0x15E3, 0x15E7, 52, GumpButtonType.Page, page - 1);
                builder.AddHtmlLocalized(170, 335, 60, 20, 1074880, 1); // Previous

                offset = 110;
            }

            var available = entry.Price <= _points;
            var half = offset + height / 2;

            if (available)
            {
                builder.AddButton(35, half - 6, 0x837, 0x838, 100 + i);
            }

            builder.AddItem(
                83 - bounds.Width / 2 - bounds.X,
                half - bounds.Height / 2 - bounds.Y,
                entry.ItemID,
                available ? entry.Hue : 995
            );

            if (entry.Tooltip != 0)
            {
                builder.AddTooltip(entry.Tooltip);
            }

            builder.AddLabel(133, half - 10, available ? 0x64 : 0x21, $"{entry.Price}");

            if (entry.Description != null)
            {
                if (entry.Description.String != null)
                {
                    builder.AddHtml(190, offset, 114, height, entry.Description.String);
                }
                else if (entry.Description.Number != 0)
                {
                    builder.AddHtmlLocalized(190, offset, 114, height, entry.Description.Number, 1);
                }
            }

            offset += height + 10;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var choice = info.ButtonID - 100;

        if (choice >= 0 && choice < _rewards.Length)
        {
            var entry = _rewards[choice];

            if (entry.Price <= _points)
            {
                RewardConfirmGump.DisplayTo(sender.Mobile, this, choice, entry);
            }
        }
    }
}

public class RewardConfirmGump : DynamicGump
{
    private readonly int _index;
    private readonly RewardGump _parent;
    private readonly IRewardEntry _entry;

    public override bool Singleton => true;

    private RewardConfirmGump(RewardGump parent, int index, IRewardEntry entry) : base(120, 50)
    {
        _parent = parent;
        _index = index;
        _entry = entry;
    }

    public static void DisplayTo(Mobile from, RewardGump parent, int index, IRewardEntry entry)
    {
        if (from?.NetState != null && parent != null && entry != null)
        {
            from.SendGump(new RewardConfirmGump(parent, index, entry));
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddImageTiled(0, 0, 348, 262, 0xA8E);
        builder.AddAlphaRegion(0, 0, 348, 262);
        builder.AddImage(0, 15, 0x27A8);
        builder.AddImageTiled(0, 30, 17, 200, 0x27A7);
        builder.AddImage(0, 230, 0x27AA);
        builder.AddImage(15, 0, 0x280C);
        builder.AddImageTiled(30, 0, 300, 17, 0x280A);
        builder.AddImage(315, 0, 0x280E);
        builder.AddImage(15, 244, 0x280C);
        builder.AddImageTiled(30, 244, 300, 17, 0x280A);
        builder.AddImage(315, 244, 0x280E);
        builder.AddImage(330, 15, 0x27A8);
        builder.AddImageTiled(330, 30, 17, 200, 0x27A7);
        builder.AddImage(330, 230, 0x27AA);
        builder.AddImage(333, 2, 0x2716);
        builder.AddImage(333, 248, 0x2716);
        builder.AddImage(2, 248, 0x2716);
        builder.AddImage(2, 2, 0x2716);

        builder.AddItem(140, 120, _entry.ItemID, _entry.Hue);

        if (_entry.Tooltip != 0)
        {
            builder.AddTooltip(_entry.Tooltip);
        }

        builder.AddHtmlLocalized(25, 22, 200, 20, 1074974, 0x7D00); // Confirm Selection
        builder.AddImage(25, 40, 0xBBF);
        builder.AddHtmlLocalized(25, 55, 300, 120, 1074975, 0x7FFF); // Are you sure you wish to select this?
        builder.AddRadio(25, 175, 0x25F8, 0x25FB, true, 1);
        builder.AddRadio(25, 210, 0x25F8, 0x25FB, false, 0);
        builder.AddHtmlLocalized(60, 180, 280, 20, 1074976, 0x7FFF); // Yes
        builder.AddHtmlLocalized(60, 215, 280, 20, 1074977, 0x7FFF); // No
        builder.AddButton(265, 220, 0xF7, 0xF8, 7);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 7 && info.IsSwitched(1))
        {
            _parent.OnPicked(sender.Mobile, _index);
        }
        else
        {
            RewardGump.DisplayTo(sender.Mobile, _parent.Title, _parent.Rewards, _parent.Points, _parent.OnPicked);
        }
    }
}
