using Server.Gumps;
using Server.Network;

namespace Server.Engines.AdvancedSearch;

public abstract class AdvancedSearchWarningGump : Gump
{
    public AdvancedSearchWarningGump(
        string header, int headerColor, string content, int contentColor, int width, int height
    ) : base((640 - width) / 2, (480 - height) / 2)
    {
        Closable = false;

        AddPage(0);

        AddBackground(0, 0, width, height, 5054);

        AddImageTiled(10, 10, width - 20, 20, 2624);
        AddAlphaRegion(10, 10, width - 20, 20);
        AddHtml(
            10,
            10,
            width - 20,
            20,
            header.Color(headerColor)
        );

        AddImageTiled(10, 40, width - 20, height - 80, 2624);
        AddAlphaRegion(10, 40, width - 20, height - 80);

        if (!string.IsNullOrWhiteSpace(content))
        {
            AddHtml(
                10,
                40,
                width - 20,
                height - 80,
                content.Color(contentColor),
                scrollbar: true
            );
        }

        AddImageTiled(10, height - 30, width - 20, 20, 2624);
        AddAlphaRegion(10, height - 30, width - 20, 20);

        AddButton(10, height - 30, 4005, 4007, 1);
        AddHtmlLocalized(40, height - 30, 170, 20, 1011036, 32767); // OKAY

        AddButton(10 + (width - 20) / 2, height - 30, 4005, 4007, 0);
        AddHtmlLocalized(40 + (width - 20) / 2, height - 30, 170, 20, 1011012, 32767); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info) => OnClickResponse(sender, info.ButtonID == 1);

    protected abstract void OnClickResponse(NetState sender, bool okay);
}
