using System;
using System.Collections.Generic;
using Server;

namespace Server.Engines.MLQuests
{
	[AttributeUsage( AttributeTargets.Class )]
	public class QuesterNameAttribute : Attribute
	{
		private string m_QuesterName;

		public string QuesterName { get { return m_QuesterName; } }

		public QuesterNameAttribute( string questerName )
		{
			m_QuesterName = questerName;
		}

		private static readonly Type m_Type = typeof( QuesterNameAttribute );
		private static readonly Dictionary<Type, string> m_Cache = new Dictionary<Type, string>();

		public static string GetQuesterNameFor( Type t )
		{
			if ( t == null )
				return "";

			string result;

			if ( m_Cache.TryGetValue( t, out result ) )
				return result;

			object[] attributes = t.GetCustomAttributes( m_Type, false );

			if ( attributes.Length != 0 )
				result = ( (QuesterNameAttribute)attributes[0] ).QuesterName;
			else
				result = t.Name;

			return ( m_Cache[t] = result );
		}
	}
}
