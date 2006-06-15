using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Chat
{
	public class ChatSystem
	{
		public static void Initialize()
		{
			EventSink.ChatRequest += new ChatRequestEventHandler( EventSink_ChatRequest );
		}

		private static void EventSink_ChatRequest( ChatRequestEventArgs e )
		{
			e.Mobile.SendMessage( "Chat is not currently supported." );
		}
	}
}