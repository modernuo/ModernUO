using Server.Gumps;
using Server.Network;

namespace Server.Items;

public class AquariumGump : DynamicGump
{
    private readonly Aquarium _aquarium;
    private readonly bool _edit;

    public override bool Singleton => true;

    private AquariumGump(Aquarium aquarium, bool edit) : base(100, 100)
    {
        _aquarium = aquarium;
        _edit = edit;
    }

    public static void DisplayTo(Mobile from, Aquarium aquarium)
    {
        if (from?.NetState == null || aquarium?.Deleted != false)
        {
            return;
        }

        from.SendGump(new AquariumGump(aquarium, aquarium.HasAccess(from)));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 350, 323, 0xE10);
        builder.AddImage(0, 0, 0x2C96);

        for (var i = 1; i <= _aquarium.Items.Count; i++)
        {
            DisplayPage(ref builder, i, _edit);
        }
    }

    private void DisplayPage(ref DynamicGumpBuilder builder, int page, bool edit)
    {
        builder.AddPage(page);

        var item = _aquarium.Items[page - 1];

        // item name
        if (item.LabelNumber != 0)
        {
            builder.AddHtmlLocalized(20, 217, 250, 20, item.LabelNumber, 0x7FFF); // Name
        }

        // item details
        if (item is BaseFish fish)
        {
            builder.AddHtmlLocalized(20, 239, 315, 20, fish.GetDescription(), 0x7FFF);
        }
        else
        {
            builder.AddHtmlLocalized(20, 239, 315, 20, 1073634, 0x7FFF); // An aquarium decoration
        }

        // item image
        builder.AddItem(150, 80, item.ItemID, item.Hue);

        // item number / all items
        builder.AddHtml(20, 195, 250, 20, Html.Color($"{page}/{_aquarium.Items.Count}", 0xFFFFFF));

        // remove item
        if (edit)
        {
            builder.AddBackground(230, 195, 100, 26, 0x13BE);
            builder.AddButton(235, 200, 0x845, 0x846, page);
            builder.AddHtmlLocalized(260, 198, 60, 26, 1073838, 0x0); // Remove
        }

        // next page
        if (page < _aquarium.Items.Count)
        {
            builder.AddButton(195, 280, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page + 1);
            builder.AddHtmlLocalized(230, 283, 100, 18, 1044045, 0x7FFF); // NEXT PAGE
        }

        // previous page
        if (page > 1)
        {
            builder.AddButton(45, 280, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page - 1);
            builder.AddHtmlLocalized(80, 283, 100, 18, 1044044, 0x7FFF); // PREV PAGE
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_aquarium?.Deleted != false)
        {
            return;
        }

        var edit = _aquarium.HasAccess(sender.Mobile);

        if (info.ButtonID > 0 && info.ButtonID <= _aquarium.Items.Count && edit)
        {
            _aquarium.RemoveItem(sender.Mobile, info.ButtonID - 1);
        }

        if (info.ButtonID > 0)
        {
            sender.Mobile.SendGump(this);
        }
    }
}
