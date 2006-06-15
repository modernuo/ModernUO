using System;
using System.Net.Mail;
using System.Threading;
using Server;

namespace Server.Misc
{
	public class Email
	{
		/* In order to support emailing, fill in EmailServer:
		 * Example:
		 *  public static readonly string EmailServer = "mail.domain.com";
		 * 
		 * If you want to add crash reporting emailing, fill in CrashAddresses:
		 * Example:
		 *  public static readonly string CrashAddresses = "first@email.here;second@email.here;third@email.here";
		 * 
		 * If you want to add speech log page emailing, fill in SpeechLogPageAddresses:
		 * Example:
		 *  public static readonly string SpeechLogPageAddresses = "first@email.here;second@email.here;third@email.here";
		 */

		public static readonly string EmailServer = null;

		public static readonly string CrashAddresses = null;
		public static readonly string SpeechLogPageAddresses = null;

		public static SmtpClient Client;

		public static void Configure()
		{
			if ( EmailServer != null )
				Client = new SmtpClient( EmailServer );
		}

		public static bool Send( MailMessage message )
		{
			try
			{
				Client.Send( message );
			}
			catch
			{
				return false;
			}

			return true;
		}

		public static void AsyncSend( MailMessage message )
		{
			ThreadPool.QueueUserWorkItem( new WaitCallback( SendCallback ), message );
		}

		private static void SendCallback( object state )
		{
			MailMessage message = (MailMessage) state;

			if ( Send( message ) )
				Console.WriteLine( "Sent e-mail '{0}' to '{1}'.", message.Subject, message.To );
			else
				Console.WriteLine( "Failure sending e-mail '{0}' to '{1}'.", message.Subject, message.To );
		}
	}
}