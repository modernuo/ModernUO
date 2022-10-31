using System;
using Server.Items;

namespace Server.Engines.Craft;

public class DefCartography : CraftSystem
{
    public static void Initialize()
    {
        CraftSystem = new DefCartography();
    }

    private DefCartography() : base(1, 1, 1.25)
    {
    }

    public override SkillName MainSkill => SkillName.Cartography;

    public override TextDefinition GumpTitle { get; } = 1044008;

    public static CraftSystem CraftSystem { get; private set; }

    public override double GetChanceAtMin(CraftItem item) => 0.0;

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        return 0;
    }

    public override void PlayCraftEffect(Mobile from)
    {
        from.PlaySound(0x249);
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
        AddCraft(typeof(LocalMap), 1044448, 1015230, 10.0, 70.0, typeof(BlankMap), 1044449, 1, 1044450);
        AddCraft(typeof(CityMap), 1044448, 1015231, 25.0, 85.0, typeof(BlankMap), 1044449, 1, 1044450);
        AddCraft(typeof(SeaChart), 1044448, 1015232, 35.0, 95.0, typeof(BlankMap), 1044449, 1, 1044450);
        AddCraft(typeof(WorldMap), 1044448, 1015233, 39.5, 99.5, typeof(BlankMap), 1044449, 1, 1044450);
    }
}
