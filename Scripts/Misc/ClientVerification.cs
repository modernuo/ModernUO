using System;
using Server;
using System.Diagnostics;
using System.IO;

namespace Server.Misc
{
	public class ClientVerification
	{
		private static bool m_DetectClientRequirement = false;
		public static void Initialize()
		{
			//ClientVersion.Required = null;
			//ClientVersion.Required = new ClientVersion( "3.0.8q" );

			if( m_DetectClientRequirement )
			{
				string path = Core.FindDataFile( "client.exe" );

				if( File.Exists( path ) )
				{
					FileVersionInfo info = FileVersionInfo.GetVersionInfo( path );

					if ( info.FileMajorPart != 0 || info.FileMinorPart != 0 || info.FileBuildPart != 0 || info.FilePrivatePart != 0 )
					{
						ClientVersion.Required = new ClientVersion( info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart );
						Console.WriteLine( "Restricting client version to {0}", ClientVersion.Required );
					}
				}
			}

			ClientVersion.AllowGod = true;
			ClientVersion.AllowUOTD = true;
			ClientVersion.AllowRegular = true;

			ClientVersion.KickDelay = TimeSpan.FromSeconds( 10.0 );
		}
	}
}