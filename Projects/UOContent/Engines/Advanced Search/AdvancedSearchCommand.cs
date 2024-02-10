namespace Server.Engines.AdvancedSearch;

public static class AdvancedSearchCommand
{
    public static void Configure()
    {
        CommandSystem.Register("XmlFind", AccessLevel.Administrator, OnCommand); // For those old school peeps!
    }

    [Usage("AdvancedSearch")]
    [Description("Opens the advanced search gump.")]
    [Aliases("AdvSrch", "AS")]
    private static void OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        from.CloseGump<AdvancedSearchGump>();
        from.SendGump(new AdvancedSearchGump(from));
    }
}
