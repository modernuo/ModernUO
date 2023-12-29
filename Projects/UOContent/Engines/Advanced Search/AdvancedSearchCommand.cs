namespace Server.Engines.AdvancedSearch;

public static class AdvancedSearchCommand
{
    public static void Initialize()
    {
        CommandSystem.Register("XmlFind", AccessLevel.Player, OnCommand); // For those old school peeps!
        CommandSystem.Register("AdvancedSearch", AccessLevel.Player, OnCommand);
        CommandSystem.Register("AdvSrch", AccessLevel.Player, OnCommand);
        CommandSystem.Register("AS", AccessLevel.Player, OnCommand);
    }

    [Usage("AdvancedSearch")]
    [Description("Opens the advanced search gump.")]
    private static void OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        from.CloseGump<AdvancedSearchGump>();
        from.SendGump(new AdvancedSearchGump(from));
    }
}
