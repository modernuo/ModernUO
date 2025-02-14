using Server.Items;
using Server.Network;

namespace Server.Gumps;

public abstract class SelectAddonDirectionGump<T> : StaticGump<T> where T : SelectAddonDirectionGump<T>
{
    private readonly IDirectionAddonDeed _deed;

    public SelectAddonDirectionGump(IDirectionAddonDeed deed) : base(60, 63) => _deed = deed;

    public override bool Singleton => false;

    public abstract int SelectionNumber { get; }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 273, 324, 0x13BE);
        builder.AddImageTiled(10, 10, 253, 20, 0xA40);
        builder.AddImageTiled(10, 40, 253, 244, 0xA40);
        builder.AddImageTiled(10, 294, 253, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 253, 304);

        builder.AddButton(10, 294, 0xFB1, 0xFB2, 0);

        builder.AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF);        // CANCEL
        builder.AddHtmlLocalized(14, 12, 273, 20, SelectionNumber, 0x7FFF);

        builder.AddPage(1);

        builder.AddButton(19, 49, 0x845, 0x846, 1);
        builder.AddHtmlLocalized(44, 47, 213, 20, 1075386, 0x7FFF); // South
        builder.AddButton(19, 73, 0x845, 0x846, 2);
        builder.AddHtmlLocalized(44, 71, 213, 20, 1075387, 0x7FFF); // East
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_deed?.Deleted != false || info.ButtonID == 0)
        {
            return;
        }

        _deed.East = info.ButtonID != 1;
        _deed.SendTarget(sender.Mobile);
    }
}
