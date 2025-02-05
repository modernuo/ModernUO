using System;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Engines.Craft;

public class QueryMakersMarkGump : StaticGump<QueryMakersMarkGump>
{
    private readonly CraftItem _craftItem;
    private readonly CraftSystem _craftSystem;
    private readonly int _quality;
    private readonly BaseTool _tool;
    private readonly Type _typeRes;

    public override bool Singleton => true;

    public QueryMakersMarkGump(int quality, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool)
        : base(100, 200)
    {
        _quality = quality;
        _craftItem = craftItem;
        _craftSystem = craftSystem;
        _typeRes = typeRes;
        _tool = tool;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 220, 170, 5054);
        builder.AddBackground(10, 10, 200, 150, 3000);

        builder.AddHtmlLocalized(20, 20, 180, 80, 1018317); // Do you wish to place your maker's mark on this item?

        builder.AddHtmlLocalized(55, 100, 140, 25, 1011011); // CONTINUE
        builder.AddButton(20, 100, 4005, 4007, 1);

        builder.AddHtmlLocalized(55, 125, 140, 25, 1011012); // CANCEL
        builder.AddButton(20, 125, 4005, 4007, 0);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        var makersMark = info.ButtonID == 1;

        if (makersMark)
        {
            from.SendLocalizedMessage(501808); // You mark the item.
        }
        else
        {
            from.SendLocalizedMessage(501809); // Cancelled mark.
        }

        _craftItem.CompleteCraft(_quality, makersMark, from, _craftSystem, _typeRes, _tool, null);
    }
}
