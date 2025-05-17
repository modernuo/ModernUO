using System;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Engines.Craft;

public class CraftGump : DynamicGump
{
    public enum CraftPage
    {
        None,
        PickResource,
        PickResource2
    }

    private const int LabelHue = 0x480;
    private const int LabelColor = 0x7FFF;
    private const int FontColor = 0xFFFFFF;
    private readonly CraftSystem _craftSystem;
    private readonly Mobile _from;

    private readonly CraftPage _page;
    private readonly BaseTool _tool;
    private readonly TextDefinition _notice;

    public override bool Singleton => true;

    public CraftGump(
        Mobile from, CraftSystem craftSystem, BaseTool tool, TextDefinition notice, CraftPage page = CraftPage.None
    ) : base(40, 40)
    {
        _from = from;
        _craftSystem = craftSystem;
        _tool = tool;
        _page = page;
        _notice = notice;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var context = _craftSystem.GetContext(_from);

        builder.AddPage();

        builder.AddBackground(0, 0, 530, 437, 5054);
        builder.AddImageTiled(10, 10, 510, 22, 2624);
        builder.AddImageTiled(10, 292, 150, 45, 2624);
        builder.AddImageTiled(165, 292, 355, 45, 2624);
        builder.AddImageTiled(10, 342, 510, 85, 2624);
        builder.AddImageTiled(10, 37, 200, 250, 2624);
        builder.AddImageTiled(215, 37, 305, 250, 2624);
        builder.AddAlphaRegion(10, 10, 510, 417);

        if (_craftSystem.GumpTitle.Number > 0)
        {
            builder.AddHtmlLocalized(10, 12, 510, 20, _craftSystem.GumpTitle.Number, LabelColor);
        }
        else
        {
            builder.AddHtml(10, 12, 510, 20, _craftSystem.GumpTitle.String);
        }

        builder.AddHtmlLocalized(10, 37, 200, 22, 1044010, LabelColor);  // <CENTER>CATEGORIES</CENTER>
        builder.AddHtmlLocalized(215, 37, 305, 22, 1044011, LabelColor); // <CENTER>SELECTIONS</CENTER>
        builder.AddHtmlLocalized(10, 302, 150, 25, 1044012, LabelColor); // <CENTER>NOTICES</CENTER>

        builder.AddButton(15, 402, 4017, 4019, 0);
        builder.AddHtmlLocalized(50, 405, 150, 18, 1011441, LabelColor); // EXIT

        builder.AddButton(270, 402, 4005, 4007, GetButtonID(6, 2));
        builder.AddHtmlLocalized(305, 405, 150, 18, 1044013, LabelColor); // MAKE LAST

        // Mark option
        if (_craftSystem.MarkOption)
        {
            builder.AddButton(270, 362, 4005, 4007, GetButtonID(6, 6));
            builder.AddHtmlLocalized(
                305,
                365,
                150,
                18,
                1044017 + (int)(context?.MarkOption ?? CraftMarkOption.MarkItem), // MARK ITEM
                LabelColor
            );
        }
        // ****************************************

        // Resmelt option
        if (_craftSystem.Resmelt)
        {
            builder.AddButton(15, 342, 4005, 4007, GetButtonID(6, 1));
            builder.AddHtmlLocalized(50, 345, 150, 18, 1044259, LabelColor); // SMELT ITEM
        }
        // ****************************************

        // Repair option
        if (_craftSystem.Repair)
        {
            builder.AddButton(270, 342, 4005, 4007, GetButtonID(6, 5));
            builder.AddHtmlLocalized(305, 345, 150, 18, 1044260, LabelColor); // REPAIR ITEM
        }
        // ****************************************

        // Enhance option
        if (_craftSystem.CanEnhance)
        {
            builder.AddButton(270, 382, 4005, 4007, GetButtonID(6, 8));
            builder.AddHtmlLocalized(305, 385, 150, 18, 1061001, LabelColor); // ENHANCE ITEM
        }
        // ****************************************

        _notice.AddHtmlText(ref builder, 170, 295, 350, 40, numberColor: LabelColor, stringColor: FontColor);

        // If the system has more than one resource
        if (_craftSystem.CraftSubRes.Init)
        {
            var nameString = _craftSystem.CraftSubRes.Name.String;
            var nameNumber = _craftSystem.CraftSubRes.Name.Number;

            var resIndex = context?.LastResourceIndex ?? -1;

            var resourceType = _craftSystem.CraftSubRes.ResType;

            if (resIndex > -1)
            {
                var subResource = _craftSystem.CraftSubRes.GetAt(resIndex);

                nameString = subResource.Name.String;
                nameNumber = subResource.Name.Number;
                resourceType = subResource.ItemType;
            }

            var resourceCount = 0;

            if (_from.Backpack != null)
            {
                foreach (var item in _from.Backpack.FindItems())
                {
                    if (resourceType.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(15, 362, 4005, 4007, GetButtonID(6, 0));

            if (nameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 365, 250, 18, nameNumber, $"{resourceCount}", LabelColor);
            }
            else
            {
                builder.AddLabel(50, 365, LabelHue, $"{nameString} ({resourceCount} Available)");
            }
        }
        // ****************************************

        // For dragon scales
        if (_craftSystem.CraftSubRes2.Init)
        {
            var nameString = _craftSystem.CraftSubRes2.Name.String;
            var nameNumber = _craftSystem.CraftSubRes2.Name.Number;

            var resIndex = context?.LastResourceIndex2 ?? -1;

            var resourceType = _craftSystem.CraftSubRes2.ResType;

            if (resIndex > -1)
            {
                var subResource = _craftSystem.CraftSubRes2.GetAt(resIndex);

                nameString = subResource.Name.String;
                nameNumber = subResource.Name.Number;
                resourceType = subResource.ItemType;
            }

            var resourceCount = 0;

            if (_from.Backpack != null)
            {
                foreach (var item in _from.Backpack.FindItems())
                {
                    if (resourceType.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(15, 382, 4005, 4007, GetButtonID(6, 7));

            if (nameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 385, 250, 18, nameNumber, $"{resourceCount}", LabelColor);
            }
            else
            {
                builder.AddLabel(50, 385, LabelHue, $"{nameString} ({resourceCount} Available)");
            }
        }
        // ****************************************

        CreateGroupList(ref builder);

        if (_page == CraftPage.PickResource)
        {
            CreateResList(ref builder, false, _from);
        }
        else if (_page == CraftPage.PickResource2)
        {
            CreateResList(ref builder, true, _from);
        }
        else if (context?.LastGroupIndex > -1)
        {
            CreateItemList(ref builder, context.LastGroupIndex);
        }
    }

    public void CreateResList(ref DynamicGumpBuilder builder, bool opt, Mobile from)
    {
        var res = opt ? _craftSystem.CraftSubRes2 : _craftSystem.CraftSubRes;

        for (var i = 0; i < res.Count; ++i)
        {
            var index = i % 10;

            var subResource = res[i];

            if (index == 0)
            {
                if (i > 0)
                {
                    builder.AddButton(485, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                }

                builder.AddPage(i / 10 + 1);

                if (i > 0)
                {
                    builder.AddButton(455, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                }

                var context = _craftSystem.GetContext(_from);

                builder.AddButton(220, 260, 4005, 4007, GetButtonID(6, 4));
                builder.AddHtmlLocalized(
                    255,
                    263,
                    200,
                    18,
                    context?.DoNotColor != true ? 1061591 : 1061590,
                    LabelColor
                );
            }

            var resourceCount = 0;

            if (from.Backpack != null)
            {
                var type = subResource.ItemType;
                foreach (var item in from.Backpack.FindItems())
                {
                    if (type.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(5, i));

            if (subResource.Name.Number > 0)
            {
                builder.AddHtmlLocalized(
                    255,
                    63 + index * 20,
                    250,
                    18,
                    subResource.Name.Number,
                    $"{resourceCount}",
                    LabelColor
                );
            }
            else
            {
                builder.AddLabel(255, 63 + index * 20, LabelHue, $"{subResource.Name.String} ({resourceCount})");
            }
        }
    }

    public void CreateMakeLastList(ref DynamicGumpBuilder builder)
    {
        var context = _craftSystem.GetContext(_from);

        if (context == null)
        {
            return;
        }

        var items = context.Items;

        if (items.Count > 0)
        {
            for (var i = 0; i < items.Count; ++i)
            {
                var index = i % 10;

                var craftItem = items[i];

                if (index == 0)
                {
                    if (i > 0)
                    {
                        builder.AddButton(370, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                        builder.AddHtmlLocalized(405, 263, 100, 18, 1044045, LabelColor); // NEXT PAGE
                    }

                    builder.AddPage(i / 10 + 1);

                    if (i > 0)
                    {
                        builder.AddButton(220, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                        builder.AddHtmlLocalized(255, 263, 100, 18, 1044044, LabelColor); // PREV PAGE
                    }
                }

                builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(3, i));

                if (craftItem.NameNumber > 0)
                {
                    builder.AddHtmlLocalized(255, 63 + index * 20, 220, 18, craftItem.NameNumber, LabelColor);
                }
                else
                {
                    builder.AddLabel(255, 63 + index * 20, LabelHue, craftItem.NameString);
                }

                builder.AddButton(480, 60 + index * 20, 4011, 4012, GetButtonID(4, i));
            }
        }
        else
        {
            builder.AddHtmlLocalized(230, 62, 200, 22, 1044165, LabelColor); // You haven't made anything yet.
        }
    }

    public void CreateItemList(ref DynamicGumpBuilder builder, int selectedGroup)
    {
        if (selectedGroup == 501) // 501 : Last 10
        {
            CreateMakeLastList(ref builder);
            return;
        }

        var craftGroup = _craftSystem.CraftGroups[selectedGroup];
        var craftItemCol = craftGroup.CraftItems;

        for (var i = 0; i < craftItemCol.Count; ++i)
        {
            var index = i % 10;

            var craftItem = craftItemCol[i];

            if (index == 0)
            {
                if (i > 0)
                {
                    builder.AddButton(370, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                    builder.AddHtmlLocalized(405, 263, 100, 18, 1044045, LabelColor); // NEXT PAGE
                }

                builder.AddPage(i / 10 + 1);

                if (i > 0)
                {
                    builder.AddButton(220, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                    builder.AddHtmlLocalized(255, 263, 100, 18, 1044044, LabelColor); // PREV PAGE
                }
            }

            builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(1, i));

            if (craftItem.NameNumber > 0)
            {
                builder.AddHtmlLocalized(255, 63 + index * 20, 220, 18, craftItem.NameNumber, LabelColor);
            }
            else
            {
                builder.AddLabel(255, 63 + index * 20, LabelHue, craftItem.NameString);
            }

            builder.AddButton(480, 60 + index * 20, 4011, 4012, GetButtonID(2, i));
        }
    }

    public void CreateGroupList(ref DynamicGumpBuilder builder)
    {
        var craftGroupCol = _craftSystem.CraftGroups;

        builder.AddButton(15, 60, 4005, 4007, GetButtonID(6, 3));
        builder.AddHtmlLocalized(50, 63, 150, 18, 1044014, LabelColor); // LAST TEN

        for (var i = 0; i < craftGroupCol.Count; i++)
        {
            var craftGroup = craftGroupCol[i];

            builder.AddButton(15, 80 + i * 20, 4005, 4007, GetButtonID(0, i));

            if (craftGroup.NameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 83 + i * 20, 150, 18, craftGroup.NameNumber, LabelColor);
            }
            else
            {
                builder.AddLabel(50, 83 + i * 20, LabelHue, craftGroup.NameString);
            }
        }
    }

    public override void SendTo(NetState ns)
    {
        _from.CloseGump<CraftGumpItem>();

        base.SendTo(ns);
    }

    public static int GetButtonID(int type, int index) => 1 + type + index * 7;

    public void CraftItem(CraftItem item)
    {
        var num = _craftSystem.CanCraft(_from, _tool, item.ItemType);

        if (num > 0)
        {
            _from.SendGump(new CraftGump(_from, _craftSystem, _tool, num));
        }
        else
        {
            Type type = null;

            var context = _craftSystem.GetContext(_from);

            if (context != null)
            {
                var res = item.UseSubRes2 ? _craftSystem.CraftSubRes2 : _craftSystem.CraftSubRes;
                var resIndex = item.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;

                if (resIndex >= 0 && resIndex < res.Count)
                {
                    type = res.GetAt(resIndex).ItemType;
                }
            }

            _craftSystem.CreateItem(_from, item.ItemType, type, _tool, item);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID <= 0)
        {
            return; // Canceled
        }

        var buttonID = info.ButtonID - 1;
        var type = buttonID % 7;
        var index = buttonID / 7;

        var groups = _craftSystem.CraftGroups;
        var context = _craftSystem.GetContext(_from);

        switch (type)
        {
            case 0: // Show group
                {
                    if (context == null)
                    {
                        break;
                    }

                    if (index >= 0 && index < groups.Count)
                    {
                        context.LastGroupIndex = index;
                        _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null));
                    }

                    break;
                }
            case 1: // Create item
                {
                    if (context == null)
                    {
                        break;
                    }

                    var groupIndex = context.LastGroupIndex;

                    if (groupIndex >= 0 && groupIndex < groups.Count)
                    {
                        var group = groups[groupIndex];

                        if (index >= 0 && index < group.CraftItems.Count)
                        {
                            CraftItem(group.CraftItems[index]);
                        }
                    }

                    break;
                }
            case 2: // Item details
                {
                    if (context == null)
                    {
                        break;
                    }

                    var groupIndex = context.LastGroupIndex;

                    if (groupIndex >= 0 && groupIndex < groups.Count)
                    {
                        var group = groups[groupIndex];

                        if (index >= 0 && index < group.CraftItems.Count)
                        {
                            _from.SendGump(new CraftGumpItem(_from, _craftSystem, group.CraftItems[index], _tool));
                        }
                    }

                    break;
                }
            case 3: // Create item (last 10)
                {
                    if (context == null)
                    {
                        break;
                    }

                    var lastTen = context.Items;

                    if (index >= 0 && index < lastTen.Count)
                    {
                        CraftItem(lastTen[index]);
                    }

                    break;
                }
            case 4: // Item details (last 10)
                {
                    if (context == null)
                    {
                        break;
                    }

                    var lastTen = context.Items;

                    if (index >= 0 && index < lastTen.Count)
                    {
                        _from.SendGump(new CraftGumpItem(_from, _craftSystem, lastTen[index], _tool));
                    }

                    break;
                }
            case 5: // Resource selected
                {
                    if (_page == CraftPage.PickResource && index >= 0 && index < _craftSystem.CraftSubRes.Count)
                    {
                        var res = _craftSystem.CraftSubRes.GetAt(index);

                        if (_from.Skills[_craftSystem.MainSkill].Base < res.RequiredSkill)
                        {
                            _from.SendGump(new CraftGump(_from, _craftSystem, _tool, res.Message));
                        }
                        else
                        {
                            if (context != null)
                            {
                                context.LastResourceIndex = index;
                            }

                            _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null));
                        }
                    }
                    else if (_page == CraftPage.PickResource2 && index >= 0 && index < _craftSystem.CraftSubRes2.Count)
                    {
                        var res = _craftSystem.CraftSubRes2.GetAt(index);

                        if (_from.Skills[_craftSystem.MainSkill].Base < res.RequiredSkill)
                        {
                            _from.SendGump(new CraftGump(_from, _craftSystem, _tool, res.Message));
                        }
                        else
                        {
                            if (context != null)
                            {
                                context.LastResourceIndex2 = index;
                            }

                            _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null));
                        }
                    }

                    break;
                }
            case 6: // Misc. buttons
                {
                    switch (index)
                    {
                        case 0: // Resource selection
                            {
                                if (_craftSystem.CraftSubRes.Init)
                                {
                                    _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null, CraftPage.PickResource));
                                }

                                break;
                            }
                        case 1: // Smelt item
                            {
                                if (_craftSystem.Resmelt)
                                {
                                    Resmelt.Do(_from, _craftSystem, _tool);
                                }

                                break;
                            }
                        case 2: // Make last
                            {
                                if (context == null)
                                {
                                    break;
                                }

                                var item = context.LastMade;

                                if (item != null)
                                {
                                    CraftItem(item);
                                }
                                else
                                {
                                    _from.SendGump(
                                        new CraftGump(
                                            _from,
                                            _craftSystem,
                                            _tool,
                                            1044165, // You haven't made anything yet.
                                            _page
                                        )
                                    );
                                }

                                break;
                            }
                        case 3: // Last 10
                            {
                                if (context == null)
                                {
                                    break;
                                }

                                context.LastGroupIndex = 501;
                                _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null));

                                break;
                            }
                        case 4: // Toggle use resource hue
                            {
                                if (context == null)
                                {
                                    break;
                                }

                                context.DoNotColor = !context.DoNotColor;

                                _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null, _page));

                                break;
                            }
                        case 5: // Repair item
                            {
                                if (_craftSystem.Repair)
                                {
                                    Repair.Do(_from, _craftSystem, _tool);
                                }

                                break;
                            }
                        case 6: // Toggle mark option
                            {
                                if (context == null || !_craftSystem.MarkOption)
                                {
                                    break;
                                }

                                context.MarkOption = context.MarkOption switch
                                {
                                    CraftMarkOption.MarkItem      => CraftMarkOption.DoNotMark,
                                    CraftMarkOption.DoNotMark     => CraftMarkOption.PromptForMark,
                                    CraftMarkOption.PromptForMark => CraftMarkOption.MarkItem,
                                    _                             => context.MarkOption
                                };

                                _from.SendGump(new CraftGump(_from, _craftSystem, _tool, null, _page));

                                break;
                            }
                        case 7: // Resource selection 2
                            {
                                if (_craftSystem.CraftSubRes2.Init)
                                {
                                    _from.SendGump(
                                        new CraftGump(_from, _craftSystem, _tool, null, CraftPage.PickResource2)
                                    );
                                }

                                break;
                            }
                        case 8: // Enhance item
                            {
                                if (_craftSystem.CanEnhance)
                                {
                                    Enhance.BeginTarget(_from, _craftSystem, _tool);
                                }

                                break;
                            }
                    }

                    break;
                }
        }
    }
}
