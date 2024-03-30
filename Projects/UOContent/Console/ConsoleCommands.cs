using Server.Saves;

namespace Server.Misc;

public static class ConsoleCommands
{
    public static void Configure()
    {
        ConsoleInputHandler.RegisterCommand(
            ["save", "s"],
            "Saves the world",
            _ => Core.LoopContext.Post(AutoSave.Save)
        );

        ConsoleInputHandler.RegisterCommand(
            ["shutdown", "sh"],
            "Shuts down the server.",
            _ => Core.Kill()
        );

        ConsoleInputHandler.RegisterCommand(
            ["restart", "r"],
            "Restarts the server.",
            _ => Core.Kill(true)
        );
    }
}
