using System;
using Server;

namespace Server.Network
{
	public class DisplayHelpTopic : Packet
	{
		public DisplayHelpTopic( int topicID, bool display ) : base( 0xBF )
		{
			EnsureCapacity( 11 );

			m_Stream.Write( (short) 0x17 );
			m_Stream.Write( (byte) 1 );
			m_Stream.Write( (int) topicID );
			m_Stream.Write( (bool) display );
		}
	}
}