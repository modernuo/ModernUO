using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Craft;

public class DefGlassblowing : CraftSystem
{
    public static void Initialize()
    {
        CraftSystem = new DefGlassblowing();
    }

    private DefGlassblowing() : base(1, 1, 1.25)
    {
    }

    public override SkillName MainSkill => SkillName.Alchemy;

    public override TextDefinition GumpTitle => 1044622;

    public static CraftSystem CraftSystem { get; private set; }

    public override double GetChanceAtMin(CraftItem item) => item.ItemType == typeof(HollowPrism) ? 0.5 : 0.0;

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckTool(tool, from))
        {
            return 1048146; // If you have a tool equipped, you must use that tool.
        }

        if (!(from is PlayerMobile mobile && mobile.Glassblowing && mobile.Skills.Alchemy.Base >= 100.0))
        {
            return 1044634; // You havent learned glassblowing.
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        DefBlacksmithy.CheckAnvilAndForge(from, 2, out _, out var forge);

        return forge ? 0 : 1044628; // You must be near a forge to blow glass.
    }

    public override void PlayCraftEffect(Mobile from)
    {
        from.PlaySound(0x2B); // bellows

        // if (from.Body.Type == BodyType.Human && !from.Mounted)
        // from.Animate( 9, 5, 1, true, false, 0 );

        // new InternalTimer( from ).Start();
    }

    public override int PlayEndingEffect(
        Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
        bool makersMark, CraftItem item
    )
    {
        if (toolBroken)
        {
            from.SendLocalizedMessage(1044038); // You have worn out your tool
        }

        if (failed)
        {
            if (lostMaterial)
            {
                return 1044043; // You failed to create the item, and some of your materials are lost.
            }

            return 1044157; // You failed to create the item, but no materials were lost.
        }

        from.PlaySound(0x41); // glass breaking

        if (quality == 0)
        {
            return 502785; // You were barely able to make this item.  It's quality is below average.
        }

        if (makersMark && quality == 2)
        {
            return 1044156; // You create an exceptional quality item and affix your maker's mark.
        }

        if (quality == 2)
        {
            return 1044155; // You create an exceptional quality item.
        }

        return 1044154; // You create the item.
    }

    public override void InitCraftList()
    {
        var index = AddCraft(typeof(Bottle), 1044050, 1023854, 52.5, 102.5, typeof(Sand), 1044625, 1, 1044627);
        SetUseAllRes(index, true);

        AddCraft(typeof(SmallFlask), 1044050, 1044610, 52.5, 102.5, typeof(Sand), 1044625, 2, 1044627);
        AddCraft(typeof(MediumFlask), 1044050, 1044611, 52.5, 102.5, typeof(Sand), 1044625, 3, 1044627);
        AddCraft(typeof(CurvedFlask), 1044050, 1044612, 55.0, 105.0, typeof(Sand), 1044625, 2, 1044627);
        AddCraft(typeof(LongFlask), 1044050, 1044613, 57.5, 107.5, typeof(Sand), 1044625, 4, 1044627);
        AddCraft(typeof(LargeFlask), 1044050, 1044623, 60.0, 110.0, typeof(Sand), 1044625, 5, 1044627);
        AddCraft(typeof(EmptyOilFlask), 1044050, 1150866, 70.0, 120.0, typeof(Sand), 1044625, 5, 1044627);
        AddCraft(typeof(AniSmallBlueFlask), 1044050, 1044614, 60.0, 110.0, typeof(Sand), 1044625, 5, 1044627);
        AddCraft(typeof(AniLargeVioletFlask), 1044050, 1044615, 60.0, 110.0, typeof(Sand), 1044625, 5, 1044627);
        AddCraft(typeof(AniRedRibbedFlask), 1044050, 1044624, 60.0, 110.0, typeof(Sand), 1044625, 7, 1044627);
        AddCraft(typeof(EmptyVialsWRack), 1044050, 1044616, 65.0, 115.0, typeof(Sand), 1044625, 8, 1044627);
        AddCraft(typeof(FullVialsWRack), 1044050, 1044617, 65.0, 115.0, typeof(Sand), 1044625, 9, 1044627);
        AddCraft(typeof(SpinningHourglass), 1044050, 1044618, 75.0, 125.0, typeof(Sand), 1044625, 10, 1044627);

        if (Core.ML)
        {
            index = AddCraft(typeof(HollowPrism), 1044050, 1072895, 100.0, 150.0, typeof(Sand), 1044625, 8, 1044627);
            SetNeededExpansion(index, Expansion.ML);
        }
    }
}
