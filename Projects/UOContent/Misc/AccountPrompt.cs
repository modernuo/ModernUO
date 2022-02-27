using System;
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
            Console.WriteLine("This server has no accounts.");
            Console.Write("Do you want to create the owner account now? (y/n): ");

            var answer = Console.ReadLine();
            if (answer is "y" or "Y")
            {
                Console.WriteLine();

                Console.Write("Username: ");
                var username = Console.ReadLine();

                Console.Write("Password: ");
                var password = Console.ReadLine();

                var a = new Account(username, password)
                {
                    AccessLevel = AccessLevel.Owner
                };

                logger.Information("Owner account created: {0}", username);
                ServerAccess.AddProtectedAccount(a, true);
            }
            else
            {
                logger.Warning("No owner account created.");
            }
        }
    }
}
