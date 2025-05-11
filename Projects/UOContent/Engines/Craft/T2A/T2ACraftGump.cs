// T2A crafting system: packet-based, not gump-based
namespace Server.Engines.Craft.T2A;

public static class T2ACraftSystem
{
    static T2ACraftSystem()
    {
        // Only initialize T2A craft systems for None, T2A, or UOR expansions
        if (!Core.UOTD)
        {
            Server.Engines.Craft.DefBowFletching.Initialize();
            Server.Engines.Craft.DefBlacksmithy.Initialize();
            Server.Engines.Craft.DefAlchemy.Initialize();
        }
    }

    // Entry point to send the T2A craft menu to a client
    public static void ShowMenu(Server.Mobile from, Server.Engines.Craft.CraftSystem craftSystem, Server.Items.BaseTool tool)
    {
        // Only show T2A menus for None, T2A, or UOR expansions
        if (!(Core.Expansion == Expansion.None || Core.Expansion == Expansion.T2A || Core.Expansion == Expansion.UOR))
        {
            // Do nothing; let the default stack crafting system and gumps handle it
            return;
        }

        if (craftSystem == Server.Engines.Craft.DefBlacksmithy.CraftSystem)
        {
            if (Core.Expansion == Expansion.None)
            {
                from.SendMenu(new BlacksmithMenu(from, BlacksmithMenu.Main(from), "Main", tool));
            }
            else // T2A or UOR: allow resource selection (colored ingots)
            {
                from.SendMenu(new BlacksmithMenu(from, BlacksmithMenu.Main(from), "Main", tool));
            }
        }
        else if (craftSystem == Server.Engines.Craft.DefAlchemy.CraftSystem)
        {
            from.SendMenu(new AlchemyMenu(from, AlchemyMenu.Main(from), "Main", tool));
        }
        else if (craftSystem == Server.Engines.Craft.DefBowFletching.CraftSystem)
        {
            var entries = BowFletchingMenu.Main(from);
            if (entries.Length == 0)
            {
                from.SendAsciiMessage("You do not have the resources to craft anything.");
                return;
            }
            from.SendMenu(new BowFletchingMenu(from, entries, "Main", tool));
        }
        else if (craftSystem == Server.Engines.Craft.DefCarpentry.CraftSystem)
        {
            var entries = Server.Engines.Craft.T2A.CarpentryMenu.Main(from);
            if (entries.Length == 0)
            {
                from.SendAsciiMessage("You do not have the resources to craft anything.");
                return;
            }
            from.SendMenu(new Server.Engines.Craft.T2A.CarpentryMenu(from, entries, "Main", tool));
        }
        else if (craftSystem == Server.Engines.Craft.DefCartography.CraftSystem)
        {
            var entries = Server.Engines.Craft.T2A.CartographyMenu.Main(from);
            if (entries.Length == 0)
            {
                from.SendAsciiMessage("You do not have the resources to craft anything.");
                return;
            }
            from.SendMenu(new Server.Engines.Craft.T2A.CartographyMenu(from, entries, "Main", tool));
        }
        else if (craftSystem == Server.Engines.Craft.DefInscription.CraftSystem)
        {
            var entries = Server.Engines.Craft.T2A.InscriptionMenu.Main(from);
            if (entries.Length == 0)
            {
                from.SendAsciiMessage("You do not have the resources to craft anything.");
                return;
            }
            from.SendMenu(new Server.Engines.Craft.T2A.InscriptionMenu(from, entries, "Main", tool));
        }
        else if (craftSystem == Server.Engines.Craft.DefTailoring.CraftSystem)
        {
            Server.Engines.Craft.T2A.TailoringMenu.ResourceSelection(from, tool);
        }
        else if (craftSystem == Server.Engines.Craft.DefTinkering.CraftSystem)
        {
            Server.Engines.Craft.T2A.TinkeringMenu.ResourceSelection(from, tool);
        }
        else
        {
            from.SendAsciiMessage("This crafting skill does not have a T2A menu implemented.");
        }
    }

    // Handler for client responses to the T2A craft menu
    public static void HandleCraftMenuResponse(Server.Network.NetState state, System.Buffers.SpanReader reader)
    {
        // TODO: Handle 0xBF 0x000D packet (craft menu response)
    }
}
