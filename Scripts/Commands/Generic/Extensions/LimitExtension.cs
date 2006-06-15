using System;
using System.Collections;
using System.Text;

namespace Server.Commands.Generic
{
	public sealed class LimitExtension : BaseExtension
	{
		public static ExtensionInfo ExtInfo = new ExtensionInfo( 80, "Limit", 1, delegate() { return new LimitExtension(); } );

		public static void Initialize()
		{
			ExtensionInfo.Register( ExtInfo );
		}

		public override ExtensionInfo Info
		{
			get { return ExtInfo; }
		}

		private int m_Limit;

		public int Limit
		{
			get { return m_Limit; }
		}

		public LimitExtension()
		{
		}

		public override void Parse( Mobile from, string[] arguments, int offset, int size )
		{
			m_Limit = Utility.ToInt32( arguments[offset] );

			if ( m_Limit < 0 )
				throw new Exception( "Limit cannot be less than zero." );
		}

		public override void Filter( ArrayList list )
		{
			if ( list.Count > m_Limit )
				list.RemoveRange( m_Limit, list.Count - m_Limit );
		}
	}
}
