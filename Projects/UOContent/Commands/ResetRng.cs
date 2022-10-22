using Server.Random;

namespace Server.Commands;

public class ResetRng
{
    public static void Initialize()
    {
        CommandSystem.Register("RestRng", AccessLevel.Administrator, ResetRng_OnCommand);
    }

    [Usage("RestRng")]
    [Description(
        "Resets the global RNG. Use with care! Do not reset often as this will affect the over-all distribution."
    )]
    private static void ResetRng_OnCommand(CommandEventArgs e)
    {
        RandomSources.SetRng(new Xoshiro256PlusPlus());
    }
}
