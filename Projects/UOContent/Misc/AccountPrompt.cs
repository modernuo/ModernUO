using System;
using Server.Accounting;

namespace Server.Misc
{
    public static class AccountPrompt
    {
        public static void Initialize()
        {
            if (Accounts.Count == 0)
            {
                Console.WriteLine("This server has no accounts.");
                Console.Write("Do you want to create the owner account now? (y/n): ");

                var answer = Console.ReadLine();
                if (answer == "y" || answer == "Y")
                {
                    Console.WriteLine();

                    Console.Write("Username: ");
                    var username = Console.ReadLine();

                    Console.Write("Password: ");
                    var password = Console.ReadLine();

                    var a = new Account(username, password);
                    a.AccessLevel = AccessLevel.Owner;

                    Console.WriteLine("Account created.");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Account not created.");
                }
            }
        }
    }
}
