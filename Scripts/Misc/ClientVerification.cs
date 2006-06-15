using System;
using Server;

namespace Server.Misc
{
	public class ClientVerification
	{
		public static void Initialize()
		{
			ClientVersion.Required = null;
			//ClientVersion.Required = new ClientVersion( "3.0.8q" );

			ClientVersion.AllowGod = true;
			ClientVersion.AllowUOTD = true;
			ClientVersion.AllowRegular = true;

			ClientVersion.KickDelay = TimeSpan.FromSeconds( 10.0 );
		}
	}
}