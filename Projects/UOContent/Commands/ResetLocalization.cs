namespace Server.Commands;

public static class ResetLocalization
{
    public static void Configure()
    {
        CommandSystem.Register("ResetLocalization", AccessLevel.Developer, ResetLocalization_OnCommand);
    }

    [Usage("ResetLocalization [<command>]")]
    [Aliases("ResetLoc")]
    [Description(
        "Clears the localization table. Cliloc will be reloaded the next time they are used for that language."
    )]
    private static void ResetLocalization_OnCommand(CommandEventArgs e)
    {
        Localization.Clear();
        e.Mobile.SendMessage("Localization table cleared.");
    }
}
