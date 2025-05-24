using Server.Accounting;
using Server.Logging;

namespace Server.Misc;

public static class AccountPrompt
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AccountPrompt));

    public static void Initialize()
    {
        if (Accounts.Count == 0)
        {
            logger.Warning("This server has no accounts.");
            logger.Information("Do you want to create the owner account now? (y/n):");

            var answer = ConsoleInputHandler.ReadLine();
            if (answer.InsensitiveEquals("y"))
            {
                logger.Information("Input Username:");
                var username = ConsoleInputHandler.ReadLine();

                logger.Information("Input Password:");
                var password = ConsoleInputHandler.ReadLine();

                var a = new Account(username, password)
                {
                    AccessLevel = AccessLevel.Owner
                };

                logger.Information("Owner account created: {Username}", username);
                ServerAccess.AddProtectedAccount(a, true);
            }
            else
            {
                logger.Warning("No owner account created.");
            }
        }
    }
}
