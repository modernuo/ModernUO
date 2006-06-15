using System;
using Server;
using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
	public class GuildNamePrompt : Prompt
	{
		private Mobile m_Mobile;
		private Guild m_Guild;

		public GuildNamePrompt( Mobile m, Guild g )
		{
			m_Mobile = m;
			m_Guild = g;
		}

		public override void OnCancel( Mobile from )
		{
			if ( GuildGump.BadLeader( m_Mobile, m_Guild ) )
				return;

			GuildGump.EnsureClosed( m_Mobile );
			m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
		}

		public override void OnResponse( Mobile from, string text )
		{
			if ( GuildGump.BadLeader( m_Mobile, m_Guild ) )
				return;

			text = text.Trim();

			if ( text.Length > 40 )
				text = text.Substring( 0, 40 );

			if ( text.Length > 0 )
			{
				if ( Guild.FindByName( text ) != null )
				{
					m_Mobile.SendMessage( "{0} conflicts with the name of an existing guild.", text );
				}
				else
				{
					m_Guild.Name = text;
					m_Guild.GuildMessage( 1018024, true, text ); // The name of your guild has changed:
				}
			}

			GuildGump.EnsureClosed( m_Mobile );
			m_Mobile.SendGump( new GuildmasterGump( m_Mobile, m_Guild ) );
		}
	}
}