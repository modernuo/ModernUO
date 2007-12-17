using System;
using System.Reflection;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Items;
using Server.Gumps;
using CPA = Server.CommandPropertyAttribute;
using Server.Commands;
using Server.Commands.Generic;

namespace Server.Commands
{
	public enum PropertyAccess
	{
		Read		= 0x01,
		Write		= 0x02,
		ReadWrite	= Read | Write
	}

	public class Properties
	{
		public static void Register()
		{
			CommandSystem.Register( "Props", AccessLevel.Counselor, new CommandEventHandler( Props_OnCommand ) );
		}

		private class PropsTarget : Target
		{
			public PropsTarget() : base( -1, true, TargetFlags.None )
			{
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( !BaseCommand.IsAccessible( from, o ) )
					from.SendMessage( "That is not accessible." );
				else
					from.SendGump( new PropertiesGump( from, o ) );
			}
		}

		[Usage( "Props [serial]" )]
		[Description( "Opens a menu where you can view and edit all properties of a targeted (or specified) object." )]
		private static void Props_OnCommand( CommandEventArgs e )
		{
			if ( e.Length == 1 )
			{
				IEntity ent = World.FindEntity( e.GetInt32( 0 ) );

				if ( ent == null )
					e.Mobile.SendMessage( "No object with that serial was found." );
				else if ( !BaseCommand.IsAccessible( e.Mobile, ent ) )
					e.Mobile.SendMessage( "That is not accessible." );
				else
					e.Mobile.SendGump( new PropertiesGump( e.Mobile, ent ) );
			}
			else
			{
				e.Mobile.Target = new PropsTarget();
			}
		}

		private static bool CIEqual( string l, string r )
		{
			return Insensitive.Equals( l, r );
		}

		private static Type typeofCPA = typeof( CPA );

		public static CPA GetCPA( PropertyInfo p )
		{
			object[] attrs = p.GetCustomAttributes( typeofCPA, false );

			if ( attrs.Length == 0 )
				return null;

			return attrs[0] as CPA;
		}

		public static PropertyInfo[] GetPropertyInfoChain( Mobile from, Type type, string propertyString, PropertyAccess endAccess, ref string failReason )
		{
			string[] split = propertyString.Split( '.' );

			if ( split.Length == 0 )
				return null;

			PropertyInfo[] info = new PropertyInfo[split.Length];

			for ( int i = 0; i < info.Length; ++i )
			{
				string propertyName = split[i];

				if ( CIEqual( propertyName, "current" ) )
					continue;

				PropertyInfo[] props = type.GetProperties( BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public );

				bool isFinal = ( i == (info.Length - 1) );

				PropertyAccess access = endAccess;

				if ( !isFinal )
					access |= PropertyAccess.Read;

				for ( int j = 0; j < props.Length; ++j )
				{
					PropertyInfo p = props[j];

					if ( CIEqual( p.Name, propertyName ) )
					{
						CPA attr = GetCPA( p );

						if ( attr == null )
						{
							failReason = String.Format( "Property '{0}' not found.", propertyName );
							return null;
						}
						else if ( (access & PropertyAccess.Read) != 0 && from.AccessLevel < attr.ReadLevel )
						{
							failReason = String.Format( "You must be at least {0} to get the property '{1}'.",
								Mobile.GetAccessLevelName( attr.ReadLevel ), propertyName );

							return null;
						}
						else if ( (access & PropertyAccess.Write) != 0 && from.AccessLevel < attr.WriteLevel )
						{
							failReason = String.Format( "You must be at least {0} to set the property '{1}'.",
								Mobile.GetAccessLevelName( attr.WriteLevel ), propertyName );

							return null;
						}
						else if ( (access & PropertyAccess.Read) != 0 && !p.CanRead )
						{
							failReason = String.Format( "Property '{0}' is write only.", propertyName );
							return null;
						}
						else if ( (access & PropertyAccess.Write) != 0 && (!p.CanWrite || attr.ReadOnly) && isFinal )
						{
							failReason = String.Format( "Property '{0}' is read only.", propertyName );
							return null;
						}

						info[i] = p;
						type = p.PropertyType;
						break;
					}
				}

				if ( info[i] == null )
				{
					failReason = String.Format( "Property '{0}' not found.", propertyName );
					return null;
				}
			}

			return info;
		}

		public static PropertyInfo GetPropertyInfo( Mobile from, ref object obj, string propertyName, PropertyAccess access, ref string failReason )
		{
			PropertyInfo[] chain = GetPropertyInfoChain( from, obj.GetType(), propertyName, access, ref failReason );

			if ( chain == null )
				return null;

			return GetPropertyInfo( ref obj, chain, ref failReason );
		}

		public static PropertyInfo GetPropertyInfo( ref object obj, PropertyInfo[] chain, ref string failReason )
		{
			if ( chain == null || chain.Length == 0 )
			{
				failReason = "Property chain is empty.";
				return null;
			}

			for ( int i = 0; i < chain.Length - 1; ++i )
			{
				if ( chain[i] == null )
					continue;

				obj = chain[i].GetValue( obj, null );

				if ( obj == null )
				{
					failReason = String.Format( "Property '{0}' is null.", chain[i] );
					return null;
				}
			}

			return chain[chain.Length-1];
		}

		public static string GetValue( Mobile from, object o, string name )
		{
			string failReason = "";

			PropertyInfo[] chain = GetPropertyInfoChain( from, o.GetType(), name, PropertyAccess.Read, ref failReason );

			if ( chain == null || chain.Length == 0 )
				return failReason;

			PropertyInfo p = GetPropertyInfo( ref o, chain, ref failReason );

			if ( p == null )
				return failReason;

			return InternalGetValue( o, p, chain );
		}

		public static string IncreaseValue( Mobile from, object o, string[] args )
		{
			Type type = o.GetType();

			object[] realObjs = new object[args.Length/2];
			PropertyInfo[] realProps = new PropertyInfo[args.Length/2];
			int[] realValues = new int[args.Length/2];

			bool positive = false, negative = false;

			for ( int i = 0; i < realProps.Length; ++i )
			{
				string name = args[i*2];

				try
				{
					string valueString = args[1 + (i*2)];

					if ( valueString.StartsWith( "0x" ) )
					{
						realValues[i] = Convert.ToInt32( valueString.Substring( 2 ), 16 );
					}
					else
					{
						realValues[i] = Convert.ToInt32( valueString );
					}
				}
				catch
				{
					return "Offset value could not be parsed.";
				}

				if ( realValues[i] > 0 )
					positive = true;
				else if ( realValues[i] < 0 )
					negative = true;
				else
					return "Zero is not a valid value to offset.";

				string failReason = null;
				realObjs[i] = o;
				realProps[i] = GetPropertyInfo( from, ref realObjs[i], name, PropertyAccess.ReadWrite, ref failReason );

				if ( failReason != null )
					return failReason;

				if ( realProps[i] == null )
					return "Property not found.";
			}

			for ( int i = 0; i < realProps.Length; ++i )
			{
				object obj = realProps[i].GetValue( realObjs[i], null );
				long v = (long)Convert.ChangeType( obj, TypeCode.Int64 );
				v += realValues[i];

				realProps[i].SetValue( realObjs[i], Convert.ChangeType( v, realProps[i].PropertyType ), null );
			}

			if ( realProps.Length == 1 )
			{
				if ( positive )
					return "The property has been increased.";

				return "The property has been decreased.";
			}

			if ( positive && negative )
				return "The properties have been changed.";

			if ( positive )
				return "The properties have been increased.";

			return "The properties have been decreased.";
		}

		private static string InternalGetValue( object o, PropertyInfo p )
		{
			return InternalGetValue( o, p, null );
		}

		private static string InternalGetValue( object o, PropertyInfo p, PropertyInfo[] chain )
		{
			Type type = p.PropertyType;

			object value = p.GetValue( o, null );
			string toString;

			if ( value == null )
				toString = "null";
			else if ( IsNumeric( type ) )
				toString = String.Format( "{0} (0x{0:X})", value );
			else if ( IsChar( type ) )
				toString = String.Format( "'{0}' ({1} [0x{1:X}])", value, (int) value );
			else if ( IsString( type ) )
				toString = ( (string) value == "null" ? @"@""null""" : String.Format( "\"{0}\"", value ) );
			else
				toString = value.ToString();

			if ( chain == null )
				return String.Format( "{0} = {1}", p.Name, toString );

			string[] concat = new string[chain.Length*2+1];

			for ( int i = 0; i < chain.Length; ++i )
			{
				concat[(i*2)+0] = chain[i].Name;
				concat[(i*2)+1] = ( i < (chain.Length - 1) ) ? "." : " = ";
			}

			concat[concat.Length-1] = toString;

			return String.Concat( concat );
		}

		public static string SetValue( Mobile from, object o, string name, string value )
		{
			object logObject = o;

			string failReason = "";
			PropertyInfo p = GetPropertyInfo( from, ref o, name, PropertyAccess.Write, ref failReason );

			if ( p == null )
				return failReason;

			return InternalSetValue( from, logObject, o, p, name, value, true );
		}

		private static Type typeofSerial = typeof( Serial );

		private static bool IsSerial( Type t )
		{
			return ( t == typeofSerial );
		}

		private static Type typeofType = typeof( Type );

		private static bool IsType( Type t )
		{
			return ( t == typeofType );
		}

		private static Type typeofChar = typeof( Char );

		private static bool IsChar( Type t )
		{
			return ( t == typeofChar );
		}

		private static Type typeofString = typeof( String );

		private static bool IsString( Type t )
		{
			return ( t == typeofString );
		}

		private static bool IsEnum( Type t )
		{
			return t.IsEnum;
		}

		private static Type typeofTimeSpan = typeof( TimeSpan );
		private static Type typeofParsable = typeof( ParsableAttribute );

		private static bool IsParsable( Type t )
		{
			return ( t == typeofTimeSpan || t.IsDefined( typeofParsable, false ) );
		}

		private static Type[] m_ParseTypes = new Type[]{ typeof( string ) };
		private static object[] m_ParseParams = new object[1];

		private static object Parse( object o, Type t, string value )
		{
			MethodInfo method = t.GetMethod( "Parse", m_ParseTypes );

			m_ParseParams[0] = value;

			return method.Invoke( o, m_ParseParams );
		}

		private static Type[] m_NumericTypes = new Type[]
			{
				typeof( Byte ), typeof( SByte ),
				typeof( Int16 ), typeof( UInt16 ),
				typeof( Int32 ), typeof( UInt32 ),
				typeof( Int64 ), typeof( UInt64 )
			};

		private static bool IsNumeric( Type t )
		{
			return ( Array.IndexOf( m_NumericTypes, t ) >= 0 );
		}

		public static string ConstructFromString( Type type, object obj, string value, ref object constructed )
		{
			object toSet;
			bool isSerial = IsSerial( type );

			if ( isSerial ) // mutate into int32
				type = m_NumericTypes[4];

			if ( value == "(-null-)" && !type.IsValueType )
				value = null;

			if ( IsEnum( type ) )
			{
				try
				{
					toSet = Enum.Parse( type, value, true );
				}
				catch
				{
					return "That is not a valid enumeration member.";
				}
			}
			else if ( IsType( type ) )
			{
				try
				{
					toSet = ScriptCompiler.FindTypeByName( value );

					if ( toSet == null )
						return "No type with that name was found.";
				}
				catch
				{
					return "No type with that name was found.";
				}
			}
			else if ( IsParsable( type ) )
			{
				try
				{
					toSet = Parse( obj, type, value );
				}
				catch
				{
					return "That is not properly formatted.";
				}
			}
			else if ( value == null )
			{
				toSet = null;
			}
			else if ( value.StartsWith( "0x" ) && IsNumeric( type ) )
			{
				try
				{
					toSet = Convert.ChangeType( Convert.ToUInt64( value.Substring( 2 ), 16 ), type );
				}
				catch
				{
					return "That is not properly formatted.";
				}
			}
			else
			{
				try
				{
					toSet = Convert.ChangeType( value, type );
				}
				catch
				{
					return "That is not properly formatted.";
				}
			}

			if ( isSerial ) // mutate back
				toSet = (Serial)((Int32)toSet);

			constructed = toSet;
			return null;
		}

		public static string SetDirect( Mobile from, object logObject, object obj, PropertyInfo prop, string givenName, object toSet, bool shouldLog )
		{
			try
			{
				if ( toSet is AccessLevel )
				{
					AccessLevel newLevel = (AccessLevel) toSet;
					AccessLevel reqLevel = AccessLevel.Administrator;

					if ( newLevel == AccessLevel.Administrator )
						reqLevel = AccessLevel.Developer;
					else if ( newLevel >= AccessLevel.Developer )
						reqLevel = AccessLevel.Owner;

					if ( from.AccessLevel < reqLevel )
						return "You do not have access to that level.";
				}

				if ( shouldLog )
					CommandLogging.LogChangeProperty( from, logObject, givenName, toSet == null ? "(-null-)" : toSet.ToString() );

				prop.SetValue( obj, toSet, null );
				return "Property has been set.";
			}
			catch
			{
				return "An exception was caught, the property may not be set.";
			}
		}

		public static string InternalSetValue( Mobile from, object logobj, object o, PropertyInfo p, string pname, string value, bool shouldLog )
		{
			object toSet = null;
			string result = ConstructFromString( p.PropertyType, o, value, ref toSet );

			if ( result != null )
				return result;

			return SetDirect( from, logobj, o, p, pname, toSet, shouldLog );
		}
	}
}

namespace Server
{
	public abstract class PropertyException : ApplicationException
	{
		protected Property m_Property;

		public Property Property
		{
			get { return m_Property; }
		}

		public PropertyException( Property property, string message )
			: base( message )
		{
			m_Property = property;
		}
	}

	public abstract class BindingException : PropertyException
	{
		public BindingException( Property property, string message )
			: base( property, message )
		{
		}
	}

	public sealed class NotYetBoundException : BindingException
	{
		public NotYetBoundException( Property property )
			: base( property, String.Format( "Property has not yet been bound." ) )
		{
		}
	}

	public sealed class AlreadyBoundException : BindingException
	{
		public AlreadyBoundException( Property property )
			: base( property, String.Format( "Property has already been bound." ) )
		{
		}
	}

	public sealed class UnknownPropertyException : BindingException
	{
		public UnknownPropertyException( Property property, string current )
			: base( property, String.Format( "Property '{0}' not found.", current ) )
		{
		}
	}

	public sealed class ReadOnlyException : BindingException
	{
		public ReadOnlyException( Property property )
			: base( property, "Property is read-only." )
		{
		}
	}

	public sealed class WriteOnlyException : BindingException
	{
		public WriteOnlyException( Property property )
			: base( property, "Property is write-only." )
		{
		}
	}

	public abstract class AccessException : PropertyException
	{
		public AccessException( Property property, string message )
			: base( property, message )
		{
		}
	}

	public sealed class InternalAccessException : AccessException
	{
		public InternalAccessException( Property property )
			: base( property, "Property is internal." )
		{
		}
	}

	public abstract class ClearanceException : AccessException
	{
		protected AccessLevel m_PlayerAccess;
		protected AccessLevel m_NeededAccess;

		public AccessLevel PlayerAccess
		{
			get { return m_PlayerAccess; }
		}

		public AccessLevel NeededAccess
		{
			get { return m_NeededAccess; }
		}

		public ClearanceException( Property property, AccessLevel playerAccess, AccessLevel neededAccess, string accessType )
			: base( property, string.Format(
				"You must be at least {0} to {1} this property.",
				Mobile.GetAccessLevelName( neededAccess ),
				accessType
			) )
		{
		}
	}

	public sealed class ReadAccessException : ClearanceException
	{
		public ReadAccessException( Property property, AccessLevel playerAccess, AccessLevel neededAccess )
			: base( property, playerAccess, neededAccess, "read" )
		{
		}
	}

	public sealed class WriteAccessException : ClearanceException
	{
		public WriteAccessException( Property property, AccessLevel playerAccess, AccessLevel neededAccess )
			: base( property, playerAccess, neededAccess, "write" )
		{
		}
	}

	public sealed class Property
	{
		private string m_Binding;

		private PropertyInfo[] m_Chain;
		private PropertyAccess m_Access;

		public string Binding
		{
			get { return m_Binding; }
		}

		public bool IsBound
		{
			get { return ( m_Chain != null ); }
		}

		public PropertyAccess Access
		{
			get { return m_Access; }
		}

		public PropertyInfo[] Chain
		{
			get
			{
				if ( !IsBound )
					throw new NotYetBoundException( this );

				return m_Chain;
			}
		}

		public Type Type
		{
			get
			{
				if ( !IsBound )
					throw new NotYetBoundException( this );

				return m_Chain[m_Chain.Length - 1].PropertyType;
			}
		}

		public bool CheckAccess( Mobile from )
		{
			if ( !IsBound )
				throw new NotYetBoundException( this );

			for ( int i = 0; i < m_Chain.Length; ++i )
			{
				PropertyInfo prop = m_Chain[i];

				bool isFinal = ( i == ( m_Chain.Length - 1 ) );

				PropertyAccess access = m_Access;

				if ( !isFinal )
					access |= PropertyAccess.Read;

				CPA security = Properties.GetCPA( prop );

				if ( security == null )
					throw new InternalAccessException( this );

				if ( ( access & PropertyAccess.Read ) != 0 && from.AccessLevel < security.ReadLevel )
					throw new ReadAccessException( this, from.AccessLevel, security.ReadLevel );

				if ( ( access & PropertyAccess.Write ) != 0 && (from.AccessLevel < security.WriteLevel || security.ReadOnly) )
					throw new WriteAccessException( this, from.AccessLevel, security.ReadLevel );
			}

			return true;
		}

		public void BindTo( Type objectType, PropertyAccess desiredAccess )
		{
			if ( IsBound )
				throw new AlreadyBoundException( this );

			string[] split = m_Binding.Split( '.' );

			PropertyInfo[] chain = new PropertyInfo[split.Length];

			for ( int i = 0; i < split.Length; ++i )
			{
				bool isFinal = ( i == ( chain.Length - 1 ) );

				chain[i] = objectType.GetProperty( split[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase );

				if ( chain[i] == null )
					throw new UnknownPropertyException( this, split[i] );

				objectType = chain[i].PropertyType;

				PropertyAccess access = desiredAccess;

				if ( !isFinal )
					access |= PropertyAccess.Read;

				if ( ( access & PropertyAccess.Read ) != 0 && !chain[i].CanRead )
					throw new WriteOnlyException( this );

				if ( ( access & PropertyAccess.Write ) != 0 && !chain[i].CanWrite )
					throw new ReadOnlyException( this );
			}

			m_Access = desiredAccess;
			m_Chain = chain;
		}

		public Property( string binding )
		{
			m_Binding = binding;
		}

		public Property( PropertyInfo[] chain )
		{
			m_Chain = chain;
		}

		public override string ToString()
		{
			if ( !IsBound )
				return m_Binding;

			string[] toJoin = new string[m_Chain.Length];

			for ( int i = 0; i < toJoin.Length; ++i )
				toJoin[i] = m_Chain[i].Name;

			return string.Join( ".", toJoin );
		}

		public static Property Parse( Type type, string binding, PropertyAccess access )
		{
			Property prop = new Property( binding );

			prop.BindTo( type, access );

			return prop;
		}
	}
}