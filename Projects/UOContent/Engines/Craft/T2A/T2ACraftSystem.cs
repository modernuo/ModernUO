// T2A crafting system: packet-based, not gump-based
namespace Server.Engines.Craft.T2A;

public static class T2ACraftSystem
{
    public static void ShowMenu(Mobile from, CraftSystem craftSystem, Items.BaseTool tool)
    {
        if (Core.UOTD)
        {
            return;
        }

        if (craftSystem == DefBlacksmithy.CraftSystem)
        {
            from.SendMenu(new BlacksmithMenu(tool));
        }
        else if (craftSystem == DefAlchemy.CraftSystem)
        {
            from.SendMenu(new AlchemyMenu(tool));
        }
        else if (craftSystem == DefBowFletching.CraftSystem)
        {
            from.SendMenu(new BowFletchingMenu(tool));
        }
        else if (craftSystem == DefCarpentry.CraftSystem)
        {
            from.SendMenu(new CarpentryMenu(tool));
        }
        else if (craftSystem == DefCartography.CraftSystem)
        {
            from.SendMenu(new CartographyMenu(tool));
        }
        else if (craftSystem == DefInscription.CraftSystem)
        {
            from.SendMenu(new InscriptionMenu(tool));
        }
        else if (craftSystem == DefTailoring.CraftSystem)
        {
            TailoringMenu.ResourceSelection(from, tool);
        }
        else if (craftSystem == DefTinkering.CraftSystem)
        {
            TinkeringMenu.ResourceSelection(from, tool);
        }
    }
}
