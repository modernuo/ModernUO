using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Server.Commands.Generic
{
	public delegate BaseExtension ExtensionConstructor();

	public sealed class ExtensionInfo
	{
		private static Dictionary<string, ExtensionInfo> m_Table = new Dictionary<string, ExtensionInfo>( StringComparer.InvariantCultureIgnoreCase );

		public static Dictionary<string, ExtensionInfo> Table
		{
			get { return m_Table; }
		}

		public static void Register( ExtensionInfo ext )
		{
			m_Table[ext.m_Name] = ext;
		}

		private int m_Order;

		private string m_Name;
		private int m_Size;

		private ExtensionConstructor m_Constructor;

		public int Order
		{
			get { return m_Order; }
		}

		public string Name
		{
			get { return m_Name; }
		}

		public int Size
		{
			get { return m_Size; }
		}

		public bool IsFixedSize
		{
			get { return ( m_Size >= 0 ); }
		}

		public ExtensionConstructor Constructor
		{
			get { return m_Constructor; }
		}

		public ExtensionInfo( int order, string name, int size, ExtensionConstructor constructor )
		{
			m_Name = name;
			m_Size = size;

			m_Order = order;

			m_Constructor = constructor;
		}
	}

	public sealed class Extensions : List<BaseExtension>
	{
		public Extensions()
		{
		}

		public bool IsValid( object obj )
		{
			for ( int i = 0; i < this.Count; ++i )
			{
				if ( !this[i].IsValid( obj ) )
					return false;
			}

			return true;
		}

		public void Filter( ArrayList list )
		{
			for ( int i = 0; i < this.Count; ++i )
				this[i].Filter( list );
		}

		public static Extensions Parse( Mobile from, ref string[] args )
		{
			Extensions parsed = new Extensions();

			int size = args.Length;

			Type baseType = null;

			for ( int i = args.Length - 1; i >= 0; --i )
			{
				ExtensionInfo extInfo = null;

				if ( !ExtensionInfo.Table.TryGetValue( args[i], out extInfo ) )
					continue;

				if ( extInfo.IsFixedSize && i != ( size - extInfo.Size - 1 ) )
					throw new Exception( "Invalid extended argument count." );

				BaseExtension ext = extInfo.Constructor();

				ext.Parse( from, args, i + 1, size - i - 1 );

				if ( ext is WhereExtension )
					baseType = ( ext as WhereExtension ).Conditional.Type;

				parsed.Add( ext );

				size = i;
			}

			parsed.Sort( delegate( BaseExtension a, BaseExtension b )
			{
				return ( a.Order - b.Order );
			} );

			AssemblyEmitter emitter = null;

			foreach ( BaseExtension update in parsed )
				update.Optimize( from, baseType, ref emitter );

			if ( size != args.Length )
			{
				string[] old = args;
				args = new string[size];

				for ( int i = 0; i < args.Length; ++i )
					args[i] = old[i];
			}

			return parsed;
		}
	}

	public abstract class BaseExtension
	{
		public abstract ExtensionInfo Info { get; }

		public string Name
		{
			get { return Info.Name; }
		}

		public int Size
		{
			get { return Info.Size; }
		}

		public bool IsFixedSize
		{
			get { return Info.IsFixedSize; }
		}

		public int Order
		{
			get { return Info.Order; }
		}

		public virtual void Optimize( Mobile from, Type baseType, ref AssemblyEmitter assembly )
		{
		}

		public virtual void Parse( Mobile from, string[] arguments, int offset, int size )
		{
		}

		public virtual bool IsValid( object obj )
		{
			return true;
		}

		public virtual void Filter( ArrayList list )
		{
		}
	}
}