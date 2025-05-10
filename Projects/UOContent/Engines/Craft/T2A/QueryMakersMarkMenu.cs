using System;
using Server.Items;
using Server.Menus.Questions;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class QueryMakersMarkMenu : QuestionMenu
{
    private readonly int _quality;
    private readonly CraftItem _craftItem;
    private readonly CraftSystem _craftSystem;
    private readonly Type _typeRes;
    private readonly BaseTool _tool;
    private readonly int _resHue;

    public QueryMakersMarkMenu(
        int quality, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int resHue
    ) : base("Do you wish to place your maker's mark on this item?", ["Yes", "No"])
    {
        _quality = quality;
        _craftItem = craftItem;
        _craftSystem = craftSystem;
        _typeRes = typeRes;
        _tool = tool;
        _resHue = resHue;
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var makersMark = index == 0; // 0 = Yes

        if (makersMark)
        {
            from.SendLocalizedMessage(501808); // You mark the item.
        }
        else
        {
            from.SendLocalizedMessage(501809); // Cancelled mark.
        }

        if (_resHue >= 0)
        {
            _craftItem.CompleteCraft(_quality, makersMark, from, _craftSystem, _typeRes, _tool, null, _resHue);
        }
        else
        {
            _craftItem.CompleteCraft(_quality, makersMark, from, _craftSystem, _typeRes, _tool, null);
        }
    }
}
