using System.Collections.Generic;
using Server.Network;

namespace Server.Gumps;

public interface IRewardOption
{
    void GetOptions(RewardOptionList list);
    void OnOptionSelected(Mobile from, int choice);
}

public class RewardOptionGump : DynamicGump
{
    private readonly IRewardOption _option;
    private readonly RewardOptionList _options = new();
    private readonly int _title;

    public override bool Singleton => true;

    private RewardOptionGump(IRewardOption option, int title) : base(60, 36)
    {
        _option = option;
        _title = title;

        _option?.GetOptions(_options);
    }

    public static void DisplayTo(Mobile from, IRewardOption option, int title = 0)
    {
        if (from?.NetState == null || option == null)
        {
            return;
        }

        from.SendGump(new RewardOptionGump(option, title));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 273, 324, 0x13BE);
        builder.AddImageTiled(10, 10, 253, 20, 0xA40);
        builder.AddImageTiled(10, 40, 253, 244, 0xA40);
        builder.AddImageTiled(10, 294, 253, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 253, 304);

        builder.AddButton(10, 294, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL

        if (_title > 0)
        {
            builder.AddHtmlLocalized(14, 12, 273, 20, _title, 0x7FFF);
        }
        else
        {
            builder.AddHtmlLocalized(14, 12, 273, 20, 1080392, 0x7FFF); // Select your choice from the menu below.
        }

        builder.AddPage(1);

        for (var i = 0; i < _options.Count; i++)
        {
            builder.AddButton(19, 49 + i * 24, 0x845, 0x846, _options[i].ID);
            builder.AddHtmlLocalized(44, 47 + i * 24, 213, 20, _options[i].Cliloc, 0x7FFF);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_option != null && Contains(info.ButtonID))
        {
            _option.OnOptionSelected(sender.Mobile, info.ButtonID);
        }
    }

    private bool Contains(int chosen)
    {
        if (_options == null)
        {
            return false;
        }

        foreach (var option in _options)
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
