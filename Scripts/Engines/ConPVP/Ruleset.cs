using System;
using System.Collections;

namespace Server.Engines.ConPVP
{
	public class Ruleset
	{
		private RulesetLayout m_Layout;
		private BitArray m_Options;
		private string m_Title;

		private Ruleset m_Base;
		private ArrayList m_Flavors = new ArrayList();
		private bool m_Changed;

		public RulesetLayout Layout{ get{ return m_Layout; } }
		public BitArray Options{ get{ return m_Options; } }
		public string Title{ get{ return m_Title; } set{ m_Title = value; } }

		public Ruleset Base{ get{ return m_Base; } }
		public ArrayList Flavors{ get{ return m_Flavors; } }
		public bool Changed{ get{ return m_Changed; } set{ m_Changed = value; } }

		public void ApplyDefault( Ruleset newDefault )
		{
			m_Base = newDefault;
			m_Changed = false;

			m_Options = new BitArray( newDefault.m_Options );

			ApplyFlavorsTo( this );
		}

		public void ApplyFlavorsTo( Ruleset ruleset )
		{
			for ( int i = 0; i < m_Flavors.Count; ++i )
			{
				Ruleset flavor = (Ruleset)m_Flavors[i];

				m_Options.Or( flavor.m_Options );
			}
		}

		public void AddFlavor( Ruleset flavor )
		{
			if ( m_Flavors.Contains( flavor ) )
				return;

			m_Flavors.Add( flavor );
			m_Options.Or( flavor.m_Options );
		}

		public void RemoveFlavor( Ruleset flavor )
		{
			if ( !m_Flavors.Contains( flavor ) )
				return;

			m_Flavors.Remove( flavor );
			m_Options.And( flavor.m_Options.Not() );
			flavor.m_Options.Not();
		}

		public void SetOptionRange( string title, bool value )
		{
			RulesetLayout layout = m_Layout.FindByTitle( title );

			if ( layout == null )
				return;

			for ( int i = 0; i < layout.TotalLength; ++i )
				m_Options[i + layout.Offset] = value;

			m_Changed = true;
		}

		public bool GetOption( string title, string option )
		{
			int index = 0;
			RulesetLayout layout = m_Layout.FindByOption( title, option, ref index );

			if ( layout == null )
				return true;

			return m_Options[layout.Offset + index];
		}

		public void SetOption( string title, string option, bool value )
		{
			int index = 0;
			RulesetLayout layout = m_Layout.FindByOption( title, option, ref index );

			if ( layout == null )
				return;

			m_Options[layout.Offset + index] = value;

			m_Changed = true;
		}

		public Ruleset( RulesetLayout layout )
		{
			m_Layout = layout;
			m_Options = new BitArray( layout.TotalLength );
		}
	}
}