using Server.Gumps;

namespace Server.Engines.AdvancedSearch;

public static class AdvancedSearchCommand
{
    public static void Configure()
    {
        CommandSystem.Register("AdvancedSearch", AccessLevel.Administrator, OnCommand); // For those old school peeps!
    }

    [Usage("AdvancedSearch")]
    [Description("Opens the advanced search gump.")]
    [Aliases("AdvSrch", "AS", "XmlFind")]
    private static void OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new AdvancedSearchGump(), true);
    }
}
