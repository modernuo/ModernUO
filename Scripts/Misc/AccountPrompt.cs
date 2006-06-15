using System;
using Server;
using Server.Accounting;

namespace Server.Misc
{
	public class AccountPrompt
	{
		// This script prompts the console for a username and password when 0 accounts have been loaded
		public static void Initialize()
		{
			if ( Accounts.Count == 0 && !Core.Service )
			{
				Console.WriteLine( "This server has no accounts." );
				Console.Write( "Do you want to create an administrator account now? (y/n)" );

				if( Console.ReadKey( true ).Key == ConsoleKey.Y )
				{
					Console.WriteLine();

					Console.Write( "Username: " );
					string username = Console.ReadLine();

					Console.Write( "Password: " );
					string password = Console.ReadLine();

					Account a = new Account( username, password );
					a.AccessLevel = AccessLevel.Owner;

					Console.WriteLine( "Account created." );
				}
				else
				{
					Console.WriteLine();

					Console.WriteLine( "Account not created." );
				}
			}
		}
	}
}