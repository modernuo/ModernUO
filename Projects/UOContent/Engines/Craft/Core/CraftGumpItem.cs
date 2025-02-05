using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Craft;

public class CraftGumpItem : DynamicGump
{
    private const int LabelHue = 0x480; // 0x384
    private const int RedLabelHue = 0x20;

    private const int LabelColor = 0x7FFF;
    private const int RedLabelColor = 0x6400;

    private const int GreyLabelColor = 0x3DEF;

    private static readonly Type typeofBlankScroll = typeof(BlankScroll);
    private static readonly Type typeofSpellScroll = typeof(SpellScroll);

    private readonly Mobile _from;
    private readonly CraftItem _craftItem;
    private readonly CraftSystem _craftSystem;
    private readonly BaseTool _tool;

    public override bool Singleton => true;

    public CraftGumpItem(Mobile from, CraftSystem craftSystem, CraftItem craftItem, BaseTool tool) : base(40, 40)
    {
        _from = from;
        _craftSystem = craftSystem;
        _craftItem = craftItem;
        _tool = tool;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 530, 417, 5054);
        builder.AddImageTiled(10, 10, 510, 22, 2624);
        builder.AddImageTiled(10, 37, 150, 148, 2624);
        builder.AddImageTiled(165, 37, 355, 90, 2624);
        builder.AddImageTiled(10, 190, 155, 22, 2624);
        builder.AddImageTiled(10, 217, 150, 53, 2624);
        builder.AddImageTiled(165, 132, 355, 80, 2624);
        builder.AddImageTiled(10, 275, 155, 22, 2624);
        builder.AddImageTiled(10, 302, 150, 53, 2624);
        builder.AddImageTiled(165, 217, 355, 80, 2624);
        builder.AddImageTiled(10, 360, 155, 22, 2624);
        builder.AddImageTiled(165, 302, 355, 80, 2624);
        builder.AddImageTiled(10, 387, 510, 22, 2624);
        builder.AddAlphaRegion(10, 10, 510, 399);

        builder.AddHtmlLocalized(170, 40, 150, 20, 1044053, LabelColor); // ITEM
        builder.AddHtmlLocalized(10, 192, 150, 22, 1044054, LabelColor); // <CENTER>SKILLS</CENTER>
        builder.AddHtmlLocalized(10, 277, 150, 22, 1044055, LabelColor); // <CENTER>MATERIALS</CENTER>
        builder.AddHtmlLocalized(10, 362, 150, 22, 1044056, LabelColor); // <CENTER>OTHER</CENTER>

        if (_craftSystem.GumpTitle.Number > 0)
        {
            builder.AddHtmlLocalized(10, 12, 510, 20, _craftSystem.GumpTitle.Number, LabelColor);
        }
        else
        {
            builder.AddHtml(10, 12, 510, 20, _craftSystem.GumpTitle.String);
        }

        builder.AddButton(15, 387, 4014, 4016, 0);
        builder.AddHtmlLocalized(50, 390, 150, 18, 1044150, LabelColor); // BACK

        var needsRecipe = _craftItem.Recipe != null && _from is PlayerMobile mobile &&
                          !mobile.HasRecipe(_craftItem.Recipe);

        if (needsRecipe)
        {
            builder.AddButton(270, 387, 4005, 4007, 0, GumpButtonType.Page);
            builder.AddHtmlLocalized(305, 390, 150, 18, 1044151, GreyLabelColor); // MAKE NOW
        }
        else
        {
            builder.AddButton(270, 387, 4005, 4007, 1);
            builder.AddHtmlLocalized(305, 390, 150, 18, 1044151, LabelColor); // MAKE NOW
        }

        if (_craftItem.NameNumber > 0)
        {
            builder.AddHtmlLocalized(330, 40, 180, 18, _craftItem.NameNumber, LabelColor);
        }
        else
        {
            builder.AddLabel(330, 40, LabelHue, _craftItem.NameString);
        }

        if (_craftItem.UseAllRes)
        {
            builder.AddHtmlLocalized(
                170,
                302,
                310,
                18,
                1048176, // Makes as many as possible at once
                LabelColor
            );
        }

        var otherCount = 1;

        DrawItem(ref builder, ref otherCount, out var showExceptionalChance);
        DrawSkill(ref builder, showExceptionalChance);
        DrawResource(ref builder, ref otherCount);

        if (_craftItem.RequiredExpansion != Expansion.None)
        {
            var supportsEx = _from.NetState?.SupportsExpansion(_craftItem.RequiredExpansion) == true;
            RequiredExpansionMessage(_craftItem.RequiredExpansion).AddHtmlText(
                ref builder,
                170,
                302 + otherCount++ * 20,
                310,
                18,
                false,
                false,
                supportsEx ? LabelColor : RedLabelColor,
                supportsEx ? LabelHue : RedLabelHue
            );
        }

        if (needsRecipe)
        {
            builder.AddHtmlLocalized(
                170,
                302 + otherCount++ * 20,
                310,
                18,
                1073620, // You have not learned this recipe.
                RedLabelColor
            );
        }
    }

    private static TextDefinition RequiredExpansionMessage(Expansion expansion)
    {
        return expansion switch
        {
            Expansion.SE => 1063363, // * Requires the "Samurai Empire" expansion
            Expansion.ML => 1072651, // * Requires the "Mondain's Legacy" expansion
            _            => $"* Requires the \"{ExpansionInfo.GetInfo(expansion).Name}\" expansion"
        };
    }

    public void DrawItem(ref DynamicGumpBuilder builder, ref int otherCount, out bool showExceptionalChance)
    {
        var type = _craftItem.ItemType;

        builder.AddItem(20, 50, _craftItem.ItemId, _craftItem.ItemHue);

        if (_craftItem.IsMarkable(type))
        {
            builder.AddHtmlLocalized(
                170,
                302 + otherCount++ * 20,
                310,
                18,
                1044059, // This item may hold its maker's mark
                LabelColor
            );

            showExceptionalChance = true;
        }
        else
        {
            showExceptionalChance = false;
        }
    }

    public void DrawSkill(ref DynamicGumpBuilder builder, bool showExceptionalChance)
    {
        for (var i = 0; i < _craftItem.Skills.Count; i++)
        {
            var skill = _craftItem.Skills[i];
            var minSkill = Math.Max(skill.MinSkill, 0);

            builder.AddHtmlLocalized(170, 132 + i * 20, 200, 18, AosSkillBonuses.GetLabel(skill.SkillToMake), LabelColor);
            builder.AddLabel(430, 132 + i * 20, LabelHue, $"{minSkill:F1}");
        }

        var res = _craftItem.UseSubRes2 ? _craftSystem.CraftSubRes2 : _craftSystem.CraftSubRes;
        var resIndex = -1;

        var context = _craftSystem.GetContext(_from);

        if (context != null)
        {
            resIndex = _craftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        }

        var chance = _craftItem.GetSuccessChance(
            _from,
            resIndex > -1 ? res.GetAt(resIndex).ItemType : null,
            _craftSystem,
            false,
            out _
        );

        builder.AddHtmlLocalized(170, 80, 250, 18, 1044057, LabelColor); // Success Chance:
        builder.AddLabel(430, 80, LabelHue, $"{Math.Clamp(chance, 0, 1) * 100:F1}%");

        if (showExceptionalChance)
        {
            var exceptChance = Math.Clamp(_craftItem.GetExceptionalChance(_craftSystem, chance, _from), 0, 1.0);

            builder.AddHtmlLocalized(170, 100, 250, 18, 1044058, 32767); // Exceptional Chance:
            builder.AddLabel(430, 100, LabelHue, $"{exceptChance * 100:F1}%");
        }
    }

    public void DrawResource(ref DynamicGumpBuilder builder, ref int otherCount)
    {
        var retainedColor = false;

        var context = _craftSystem.GetContext(_from);

        var res = _craftItem.UseSubRes2 ? _craftSystem.CraftSubRes2 : _craftSystem.CraftSubRes;
        var resIndex = -1;

        if (context != null)
        {
            resIndex = _craftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        }

        var cropScroll = _craftItem.Resources.Count > 1
                         && _craftItem.Resources[^1].ItemType == typeofBlankScroll
                         && typeofSpellScroll.IsAssignableFrom(_craftItem.ItemType);

        for (var i = 0; i < _craftItem.Resources.Count - (cropScroll ? 1 : 0) && i < 4; i++)
        {
            var craftResource = _craftItem.Resources[i];

            var type = craftResource.ItemType;
            var nameString = craftResource.Name.String;
            var nameNumber = craftResource.Name.Number;

            // Resource Mutation
            if (type == res.ResType && resIndex > -1)
            {
                var subResource = res.GetAt(resIndex);

                type = subResource.ItemType;

                nameString = subResource.Name.String;
                nameNumber = subResource.GenericNameNumber;

                if (nameNumber <= 0)
                {
                    nameNumber = subResource.Name.Number;
                }
            }
            // ******************

            if (!retainedColor && _craftItem.RetainsColorFrom(_craftSystem, type))
            {
                retainedColor = true;
                builder.AddHtmlLocalized(
                    170,
                    302 + otherCount++ * 20,
                    310,
                    18,
                    1044152, // * The item retains the color of this material
                    LabelColor
                );
                builder.AddLabel(500, 219 + i * 20, LabelHue, "*");
            }

            if (nameNumber > 0)
            {
                builder.AddHtmlLocalized(170, 219 + i * 20, 310, 18, nameNumber, LabelColor);
            }
            else
            {
                builder.AddLabel(170, 219 + i * 20, LabelHue, nameString);
            }

            builder.AddLabel(430, 219 + i * 20, LabelHue, craftResource.Amount.ToString());
        }

        if (_craftItem.NameNumber == 1041267) // runebook
        {
            builder.AddHtmlLocalized(170, 219 + _craftItem.Resources.Count * 20, 310, 18, 1044447, LabelColor);
            builder.AddLabel(430, 219 + _craftItem.Resources.Count * 20, LabelHue, "1");
        }

        if (cropScroll)
        {
            builder.AddHtmlLocalized(
                170,
                302 + otherCount++ * 20,
                360,
                18,
                1044379, // Inscribing scrolls also requires a blank scroll and mana.
                LabelColor
            );
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        // Back Button
        if (info.ButtonID == 0)
        {
            var craftGump = new CraftGump(from, _craftSystem, _tool, null);
            from.SendGump(craftGump);
        }
        else // Make Button
        {
            var num = _craftSystem.CanCraft(from, _tool, _craftItem.ItemType);

            if (num > 0)
            {
                from.SendGump(new CraftGump(from, _craftSystem, _tool, num));
            }
            else
            {
                Type type = null;

                var context = _craftSystem.GetContext(from);

                if (context != null)
                {
                    var res = _craftItem.UseSubRes2 ? _craftSystem.CraftSubRes2 : _craftSystem.CraftSubRes;
                    var resIndex = _craftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;

                    if (resIndex > -1)
                    {
                        type = res.GetAt(resIndex).ItemType;
                    }
                }

                _craftSystem.CreateItem(from, _craftItem.ItemType, type, _tool, _craftItem);
            }
        }
    }
}
