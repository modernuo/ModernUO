using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Server;
using Server.Items;
using Server.Network;
using Server.Targeting;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Commands
{
	public class Add
	{
		public static void Initialize()
		{
			CommandSystem.Register( "Tile", AccessLevel.GameMaster, new CommandEventHandler( Tile_OnCommand ) );
			CommandSystem.Register( "TileRXYZ", AccessLevel.GameMaster, new CommandEventHandler( TileRXYZ_OnCommand ) );
			CommandSystem.Register( "TileXYZ", AccessLevel.GameMaster, new CommandEventHandler( TileXYZ_OnCommand ) );
			CommandSystem.Register( "TileZ", AccessLevel.GameMaster, new CommandEventHandler( TileZ_OnCommand ) );
		}

		public static void Invoke( Mobile from, Point3D start, Point3D end, string[] args )
		{
			Invoke( from, start, end, args, null );
		}

		public static void Invoke( Mobile from, Point3D start, Point3D end, string[] args, List<Container> packs )
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat( "{0} {1} building ", from.AccessLevel, CommandLogging.Format( from ) );

			if ( start == end )
				sb.AppendFormat( "at {0} in {1}", start, from.Map );
			else
				sb.AppendFormat( "from {0} to {1} in {2}", start, end, from.Map );

			sb.Append( ":" );

			for ( int i = 0; i < args.Length; ++i )
				sb.AppendFormat( " \"{0}\"", args[i] );

			CommandLogging.WriteLine( from, sb.ToString() );

			string name = args[0];

			FixArgs( ref args );

			string[,] props = null;

			for ( int i = 0; i < args.Length; ++i )
			{
				if ( Insensitive.Equals( args[i], "set" ) )
				{
					int remains = args.Length - i - 1;

					if ( remains >= 2 )
					{
						props = new string[remains / 2, 2];

						remains /= 2;

						for ( int j = 0; j < remains; ++j )
						{
							props[j, 0] = args[i + (j * 2) + 1];
							props[j, 1] = args[i + (j * 2) + 2];
						}

						FixSetString( ref args, i );
					}

					break;
				}
			}

			Type type = ScriptCompiler.FindTypeByName( name );

			if ( !IsEntity( type ) ) {
				from.SendMessage( "No type with that name was found." );
				return;
			}

			DateTime time = DateTime.Now;

			int built = BuildObjects( from, type, start, end, args, props, packs );

			if ( built > 0 )
				from.SendMessage( "{0} object{1} generated in {2:F1} seconds.", built, built != 1 ? "s" : "", (DateTime.Now - time).TotalSeconds );
			else
				SendUsage( type, from );
		}

		public static void FixSetString( ref string[] args, int index )
		{
			string[] old = args;
			args = new string[index];

			Array.Copy( old, 0, args, 0, index );
		}

		public static void FixArgs( ref string[] args )
		{
			string[] old = args;
			args = new string[args.Length - 1];

			Array.Copy( old, 1, args, 0, args.Length );
		}

		public static int BuildObjects( Mobile from, Type type, Point3D start, Point3D end, string[] args, string[,] props, List<Container> packs )
		{
			Utility.FixPoints( ref start, ref end );

			PropertyInfo[] realProps = null;

			if ( props != null )
			{
				realProps = new PropertyInfo[props.GetLength( 0 )];

				PropertyInfo[] allProps = type.GetProperties( BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public );

				for ( int i = 0; i < realProps.Length; ++i )
				{
					PropertyInfo thisProp = null;

					string propName = props[i, 0];

					for ( int j = 0; thisProp == null && j < allProps.Length; ++j )
					{
						if ( Insensitive.Equals( propName, allProps[j].Name ) )
							thisProp = allProps[j];
					}

					if ( thisProp == null )
					{
						from.SendMessage( "Property not found: {0}", propName );
					}
					else
					{
						CPA attr = Properties.GetCPA( thisProp );

						if ( attr == null )
							from.SendMessage( "Property ({0}) not found.", propName );
						else if ( from.AccessLevel < attr.WriteLevel )
							from.SendMessage( "Setting this property ({0}) requires at least {1} access level.", propName, Mobile.GetAccessLevelName( attr.WriteLevel ) );
						else if ( !thisProp.CanWrite || attr.ReadOnly )
							from.SendMessage( "Property ({0}) is read only.", propName );
						else
							realProps[i] = thisProp;
					}
				}
			}

			ConstructorInfo[] ctors = type.GetConstructors();

			for ( int i = 0; i < ctors.Length; ++i )
			{
				ConstructorInfo ctor = ctors[i];

				if ( !IsConstructable( ctor, from.AccessLevel ) )
					continue;

				ParameterInfo[] paramList = ctor.GetParameters();

				if ( args.Length == paramList.Length )
				{
					object[] paramValues = ParseValues( paramList, args );

					if ( paramValues == null )
						continue;

					int built = Build( from, start, end, ctor, paramValues, props, realProps, packs );

					if ( built > 0 )
						return built;
				}
			}

			return 0;
		}

		public static object[] ParseValues( ParameterInfo[] paramList, string[] args )
		{
			object[] values = new object[args.Length];

			for ( int i = 0; i < args.Length; ++i )
			{
				object value = ParseValue( paramList[i].ParameterType, args[i] );

				if ( value != null )
					values[i] = value;
				else
					return null;
			}

			return values;
		}

		public static object ParseValue( Type type, string value )
		{
			try
			{
				if ( IsEnum( type ) )
				{
					return Enum.Parse( type, value, true );
				}
				else if ( IsType( type ) )
				{
					return ScriptCompiler.FindTypeByName( value );
				}
				else if ( IsParsable( type ) )
				{
					return ParseParsable( type, value );
				}
				else
				{
					object obj = value;

					if ( value != null && value.StartsWith( "0x" ) )
					{
						if ( IsSignedNumeric( type ) )
							obj = Convert.ToInt64( value.Substring( 2 ), 16 );
						else if ( IsUnsignedNumeric( type ) )
							obj = Convert.ToUInt64( value.Substring( 2 ), 16 );

						obj = Convert.ToInt32( value.Substring( 2 ), 16 );
					}

					if ( obj == null && !type.IsValueType )
						return null;
					else
						return Convert.ChangeType( obj, type );
				}
			}
			catch
			{
				return null;
			}
		}

		public static IEntity Build( Mobile from, ConstructorInfo ctor, object[] values, string[,] props, PropertyInfo[] realProps, ref bool sendError )
		{
			object built = ctor.Invoke( values );

			if ( built != null && realProps != null )
			{
				bool hadError = false;

				for ( int i = 0; i < realProps.Length; ++i )
				{
					if ( realProps[i] == null )
						continue;

					string result = Properties.InternalSetValue( from, built, built, realProps[i], props[i, 1], props[i, 1], false );

					if ( result != "Property has been set." )
					{
						if ( sendError )
							from.SendMessage( result );

						hadError = true;
					}
				}

				if ( hadError )
					sendError = false;
			}

			return (IEntity)built;
		}

		public static int Build( Mobile from, Point3D start, Point3D end, ConstructorInfo ctor, object[] values, string[,] props, PropertyInfo[] realProps, List<Container> packs )
		{
			try
			{
				Map map = from.Map;

				int objectCount = ( packs == null ? (((end.X - start.X) + 1) * ((end.Y - start.Y) + 1)) : packs.Count );

				if ( objectCount >= 20 )
					from.SendMessage( "Constructing {0} objects, please wait.", objectCount );

				bool sendError = true;

				StringBuilder sb = new StringBuilder();
				sb.Append( "Serials: " );

				if ( packs != null )
				{
					for ( int i = 0; i < packs.Count; ++i )
					{
						IEntity built = Build( from, ctor, values, props, realProps, ref sendError );

						sb.AppendFormat( "0x{0:X}; ", built.Serial.Value );
	
						if ( built is Item ) {
							Container pack = packs[i];
							pack.DropItem( (Item)built );
						}
						else if ( built is Mobile ) {
							Mobile m = (Mobile)built;
							m.MoveToWorld( new Point3D( start.X, start.Y, start.Z ), map );
						}
					}
				}
				else
				{
					for ( int x = start.X; x <= end.X; ++x )
					{
						for ( int y = start.Y; y <= end.Y; ++y )
						{
							IEntity built = Build( from, ctor, values, props, realProps, ref sendError );

							sb.AppendFormat( "0x{0:X}; ", built.Serial.Value );

							if ( built is Item ) {
								Item item = (Item)built;
								item.MoveToWorld( new Point3D( x, y, start.Z ), map );
							}
							else if ( built is Mobile ) {
								Mobile m = (Mobile)built;
								m.MoveToWorld( new Point3D( x, y, start.Z ), map );
							}
						}
					}
				}

				CommandLogging.WriteLine( from, sb.ToString() );

				return objectCount;
			}
			catch ( Exception ex )
			{
				Console.WriteLine(ex);
				return 0;
			}
		}

		public static void SendUsage( Type type, Mobile from )
		{
			ConstructorInfo[] ctors = type.GetConstructors();
			bool foundCtor = false;

			for ( int i = 0; i < ctors.Length; ++i )
			{
				ConstructorInfo ctor = ctors[i];

				if ( !IsConstructable( ctor, from.AccessLevel ) )
					continue;

				if ( !foundCtor )
				{
					foundCtor = true;
					from.SendMessage( "Usage:" );
				}

				SendCtor( type, ctor, from );
			}

			if ( !foundCtor )
				from.SendMessage( "That type is not marked constructable." );
		}

		public static void SendCtor( Type type, ConstructorInfo ctor, Mobile from )
		{
			ParameterInfo[] paramList = ctor.GetParameters();

			StringBuilder sb = new StringBuilder();

			sb.Append( type.Name );

			for ( int i = 0; i < paramList.Length; ++i )
			{
				if ( i != 0 )
					sb.Append( ',' );

				sb.Append( ' ' );

				sb.Append( paramList[i].ParameterType.Name );
				sb.Append( ' ' );
				sb.Append( paramList[i].Name );
			}

			from.SendMessage( sb.ToString() );
		}

		public class AddTarget : Target
		{
			private string[] m_Args;

			public AddTarget( string[] args ) : base( -1, true, TargetFlags.None )
			{
				m_Args = args;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				IPoint3D p = o as IPoint3D;

				if ( p != null )
				{
					if ( p is Item )
						p = ((Item)p).GetWorldTop();
					else if ( p is Mobile )
						p = ((Mobile)p).Location;

					Point3D point = new Point3D( p );
					Add.Invoke( from, point, point, m_Args );
				}
			}
		}

		private class TileState
		{
			public bool m_UseFixedZ;
			public int m_FixedZ;
			public string[] m_Args;

			public TileState( string[] args ) : this( false, 0, args )
			{
			}

			public TileState( int fixedZ, string[] args ) : this( true, fixedZ, args )
			{
			}

			public TileState( bool useFixedZ, int fixedZ, string[] args )
			{
				m_UseFixedZ = useFixedZ;
				m_FixedZ = fixedZ;
				m_Args = args;
			}
		}

		private static void TileBox_Callback( Mobile from, Map map, Point3D start, Point3D end, object state )
		{
			TileState ts = (TileState)state;

			if ( ts.m_UseFixedZ )
				start.Z = end.Z = ts.m_FixedZ;

			Invoke( from, start, end, ts.m_Args );
		}

		[Usage( "Tile <name> [params] [set {<propertyName> <value> ...}]" )]
		[Description( "Tiles an item or npc by name into a targeted bounding box. Optional constructor parameters. Optional set property list." )]
		public static void Tile_OnCommand( CommandEventArgs e )
		{
			if ( e.Length >= 1 )
				BoundingBoxPicker.Begin( e.Mobile, new BoundingBoxCallback( TileBox_Callback ), new TileState( e.Arguments ) );
			else
				e.Mobile.SendMessage( "Format: Add <type> [params] [set {<propertyName> <value> ...}]" );
		}

		[Usage( "TileRXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]" )]
		[Description( "Tiles an item or npc by name into a given bounding box, (x, y) parameters are relative to your characters position. Optional constructor parameters. Optional set property list." )]
		public static void TileRXYZ_OnCommand( CommandEventArgs e )
		{
			if ( e.Length >= 6 )
			{
				Point3D p = new Point3D( e.Mobile.X + e.GetInt32( 0 ), e.Mobile.Y + e.GetInt32( 1 ), e.Mobile.Z + e.GetInt32( 4 ) );
				Point3D p2 = new Point3D( p.X + e.GetInt32( 2 ) - 1, p.Y + e.GetInt32( 3 ) - 1, p.Z );

				string[] subArgs = new string[e.Length - 5];

				for ( int i = 0; i < subArgs.Length; ++i )
					subArgs[i] = e.Arguments[i + 5];

				Add.Invoke( e.Mobile, p, p2, subArgs );
			}
			else
			{
				e.Mobile.SendMessage( "Format: TileRXYZ <x> <y> <w> <h> <z> <type> [params] [set {<propertyName> <value> ...}]" );
			}
		}

		[Usage( "TileXYZ <x> <y> <w> <h> <z> <name> [params] [set {<propertyName> <value> ...}]" )]
		[Description( "Tiles an item or npc by name into a given bounding box. Optional constructor parameters. Optional set property list." )]
		public static void TileXYZ_OnCommand( CommandEventArgs e )
		{
			if ( e.Length >= 6 )
			{
				Point3D p = new Point3D( e.GetInt32( 0 ), e.GetInt32( 1 ), e.GetInt32( 4 ) );
				Point3D p2 = new Point3D( p.X + e.GetInt32( 2 ) - 1, p.Y + e.GetInt32( 3 ) - 1, e.GetInt32( 4 ) );

				string[] subArgs = new string[e.Length - 5];

				for ( int i = 0; i < subArgs.Length; ++i )
					subArgs[i] = e.Arguments[i + 5];

				Add.Invoke( e.Mobile, p, p2, subArgs );
			}
			else
			{
				e.Mobile.SendMessage( "Format: TileXYZ <x> <y> <w> <h> <z> <type> [params] [set {<propertyName> <value> ...}]" );
			}
		}

		[Usage( "TileZ <z> <name> [params] [set {<propertyName> <value> ...}]" )]
		[Description( "Tiles an item or npc by name into a targeted bounding box at a fixed Z location. Optional constructor parameters. Optional set property list." )]
		public static void TileZ_OnCommand( CommandEventArgs e )
		{
			if ( e.Length >= 2 )
			{
				string[] subArgs = new string[e.Length - 1];

				for ( int i = 0; i < subArgs.Length; ++i )
					subArgs[i] = e.Arguments[i + 1];

				BoundingBoxPicker.Begin( e.Mobile, new BoundingBoxCallback( TileBox_Callback ), new TileState( e.GetInt32( 0 ), subArgs ) );
			}
			else
			{
				e.Mobile.SendMessage( "Format: TileZ <z> <type> [params] [set {<propertyName> <value> ...}]" );
			}
		}

		private static Type m_EntityType = typeof( IEntity );

		public static bool IsEntity( Type t )
		{
			return m_EntityType.IsAssignableFrom( t );
		}

		private static Type m_ConstructableType = typeof( ConstructableAttribute );

		public static bool IsConstructable( ConstructorInfo ctor, AccessLevel accessLevel )
		{
			object[] attrs = ctor.GetCustomAttributes( m_ConstructableType, false );

			if ( attrs.Length == 0 )
				return false;

			return accessLevel >= ((ConstructableAttribute)attrs[0]).AccessLevel;
		}

		private static Type m_EnumType = typeof( Enum );

		public static bool IsEnum( Type type )
		{
			return type.IsSubclassOf( m_EnumType );
		}

		private static Type m_TypeType = typeof( Type );

		public static bool IsType( Type type )
		{
			return ( type == m_TypeType || type.IsSubclassOf( m_TypeType ) );
		}

		private static Type m_ParsableType = typeof( ParsableAttribute );

		public static bool IsParsable( Type type )
		{
			return type.IsDefined( m_ParsableType, false );
		}

		private static Type[] m_ParseTypes = new Type[]{ typeof( string ) };
		private static object[] m_ParseArgs = new object[1];

		public static object ParseParsable( Type type, string value )
		{
			MethodInfo method = type.GetMethod( "Parse", m_ParseTypes );

			m_ParseArgs[0] = value;

			return method.Invoke( null, m_ParseArgs );
		}

		private static Type[] m_SignedNumerics = new Type[]
			{
				typeof( Int64 ),
				typeof( Int32 ),
				typeof( Int16 ),
				typeof( SByte )
			};

		public static bool IsSignedNumeric( Type type )
		{
			for ( int i = 0; i < m_SignedNumerics.Length; ++i )
				if ( type == m_SignedNumerics[i] )
					return true;

			return false;
		}

		private static Type[] m_UnsignedNumerics = new Type[]
			{
				typeof( UInt64 ),
				typeof( UInt32 ),
				typeof( UInt16 ),
				typeof( Byte )
			};

		public static bool IsUnsignedNumeric( Type type )
		{
			for ( int i = 0; i < m_UnsignedNumerics.Length; ++i )
				if ( type == m_UnsignedNumerics[i] )
					return true;

			return false;
		}
	}
}