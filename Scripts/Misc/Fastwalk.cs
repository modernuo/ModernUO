using System;
using Server;

namespace Server.Misc
{
	// This fastwalk detection is no longer required
	// As of B36 PlayerMobile implements movement packet throttling which more reliably controls movement speeds
	public class Fastwalk
	{
		private static int  MaxSteps = 4;			// Maximum number of queued steps until fastwalk is detected
		private static bool Enabled = false;		// Is fastwalk detection enabled?
		private static bool UOTDOverride = false;	// Should UO:TD clients not be checked for fastwalk?
		private static AccessLevel AccessOverride = AccessLevel.GameMaster; // Anyone with this or higher access level is not checked for fastwalk

		public static void Initialize()
		{
			Mobile.FwdMaxSteps = MaxSteps;
			Mobile.FwdEnabled = Enabled;
			Mobile.FwdUOTDOverride = UOTDOverride;
			Mobile.FwdAccessOverride = AccessOverride;

			if ( Enabled )
				EventSink.FastWalk += new FastWalkEventHandler( OnFastWalk );
		}

		public static void OnFastWalk( FastWalkEventArgs e )
		{
			e.Blocked = true;//disallow this fastwalk
			Console.WriteLine( "Client: {0}: Fast movement detected (name={1})", e.NetState, e.NetState.Mobile.Name );
		}
	}
}
