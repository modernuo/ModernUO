using System;
using System.Reflection;
using System.Collections;
using Server;
using Server.Commands.Generic;
using Server.Network;
using Server.Menus;
using Server.Menus.Questions;
using Server.Targeting;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Gumps
{
	public class PropertiesGump : Gump
	{
		private ArrayList m_List;
		private int m_Page;
		private Mobile m_Mobile;
		private object m_Object;
		private Stack m_Stack;

		public static readonly bool OldStyle = PropsConfig.OldStyle;

		public static readonly int GumpOffsetX = PropsConfig.GumpOffsetX;
		public static readonly int GumpOffsetY = PropsConfig.GumpOffsetY;

		public static readonly int TextHue = PropsConfig.TextHue;
		public static readonly int TextOffsetX = PropsConfig.TextOffsetX;

		public static readonly int OffsetGumpID = PropsConfig.OffsetGumpID;
		public static readonly int HeaderGumpID = PropsConfig.HeaderGumpID;
		public static readonly int  EntryGumpID = PropsConfig.EntryGumpID;
		public static readonly int   BackGumpID = PropsConfig.BackGumpID;
		public static readonly int    SetGumpID = PropsConfig.SetGumpID;

		public static readonly int SetWidth = PropsConfig.SetWidth;
		public static readonly int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
		public static readonly int SetButtonID1 = PropsConfig.SetButtonID1;
		public static readonly int SetButtonID2 = PropsConfig.SetButtonID2;

		public static readonly int PrevWidth = PropsConfig.PrevWidth;
		public static readonly int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY;
		public static readonly int PrevButtonID1 = PropsConfig.PrevButtonID1;
		public static readonly int PrevButtonID2 = PropsConfig.PrevButtonID2;

		public static readonly int NextWidth = PropsConfig.NextWidth;
		public static readonly int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY;
		public static readonly int NextButtonID1 = PropsConfig.NextButtonID1;
		public static readonly int NextButtonID2 = PropsConfig.NextButtonID2;

		public static readonly int OffsetSize = PropsConfig.OffsetSize;

		public static readonly int EntryHeight = PropsConfig.EntryHeight;
		public static readonly int BorderSize = PropsConfig.BorderSize;

		private static bool PrevLabel = OldStyle, NextLabel = OldStyle;

		private static readonly int PrevLabelOffsetX = PrevWidth + 1;
		private static readonly int PrevLabelOffsetY = 0;

		private static readonly int NextLabelOffsetX = -29;
		private static readonly int NextLabelOffsetY = 0;

		private static readonly int NameWidth = 107;
		private static readonly int ValueWidth = 128;

		private static readonly int EntryCount = 15;

		private static readonly int TypeWidth = NameWidth + OffsetSize + ValueWidth;

		private static readonly int TotalWidth = OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;
		private static readonly int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

		private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
		private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;

		public PropertiesGump( Mobile mobile, object o ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Mobile = mobile;
			m_Object = o;
			m_List = BuildList();

			Initialize( 0 );
		}

		public PropertiesGump( Mobile mobile, object o, Stack stack, StackEntry parent ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Mobile = mobile;
			m_Object = o;
			m_Stack = stack;
			m_List = BuildList();

			if ( parent != null )
			{
				if ( m_Stack == null )
					m_Stack = new Stack();

				m_Stack.Push( parent );
			}

			Initialize( 0 );
		}

		public PropertiesGump( Mobile mobile, object o, Stack stack, ArrayList list, int page ) : base( GumpOffsetX, GumpOffsetY )
		{
			m_Mobile = mobile;
			m_Object = o;
			m_List = list;
			m_Stack = stack;

			Initialize( page );
		}

		private void Initialize( int page )
		{
			m_Page = page;

			int count = m_List.Count - (page * EntryCount);

			if ( count < 0 )
				count = 0;
			else if ( count > EntryCount )
				count = EntryCount;

			int lastIndex = (page * EntryCount) + count - 1;

			if ( lastIndex >= 0 && lastIndex < m_List.Count && m_List[lastIndex] == null )
				--count;

			int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (count + 1));

			AddPage( 0 );

			AddBackground( 0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID );
			AddImageTiled( BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID );

			int x = BorderSize + OffsetSize;
			int y = BorderSize + OffsetSize;

			int emptyWidth = TotalWidth - PrevWidth - NextWidth - (OffsetSize * 4) - (OldStyle ? SetWidth + OffsetSize : 0);

			if ( OldStyle )
				AddImageTiled( x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID );
			else
				AddImageTiled( x, y, PrevWidth, EntryHeight, HeaderGumpID );

			if ( page > 0 )
			{
				AddButton( x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0 );

				if ( PrevLabel )
					AddLabel( x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous" );
			}

			x += PrevWidth + OffsetSize;

			if ( !OldStyle )
				AddImageTiled( x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, HeaderGumpID );

			x += emptyWidth + OffsetSize;

			if ( !OldStyle )
				AddImageTiled( x, y, NextWidth, EntryHeight, HeaderGumpID );

			if ( (page + 1) * EntryCount < m_List.Count )
			{
				AddButton( x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 1 );

				if ( NextLabel )
					AddLabel( x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next" );
			}

			for ( int i = 0, index = page * EntryCount; i < count && index < m_List.Count; ++i, ++index )
			{
				x = BorderSize + OffsetSize;
				y += EntryHeight + OffsetSize;

				object o = m_List[index];

				if ( o == null )
				{
					AddImageTiled( x - OffsetSize, y, TotalWidth, EntryHeight, BackGumpID + 4 );
				}
				else if ( o is Type )
				{
					Type type = (Type)o;

					AddImageTiled( x, y, TypeWidth, EntryHeight, EntryGumpID );
					AddLabelCropped( x + TextOffsetX, y, TypeWidth - TextOffsetX, EntryHeight, TextHue, type.Name );
					x += TypeWidth + OffsetSize;

					if ( SetGumpID != 0 )
						AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );
				}
				else if ( o is PropertyInfo )
				{
					PropertyInfo prop = (PropertyInfo)o;

					AddImageTiled( x, y, NameWidth, EntryHeight, EntryGumpID );
					AddLabelCropped( x + TextOffsetX, y, NameWidth - TextOffsetX, EntryHeight, TextHue, prop.Name );
					x += NameWidth + OffsetSize;
					AddImageTiled( x, y, ValueWidth, EntryHeight, EntryGumpID );
					AddLabelCropped( x + TextOffsetX, y, ValueWidth - TextOffsetX, EntryHeight, TextHue, ValueToString( prop ) );
					x += ValueWidth + OffsetSize;

					if ( SetGumpID != 0 )
						AddImageTiled( x, y, SetWidth, EntryHeight, SetGumpID );

					CPA cpa = GetCPA( prop );

					if ( prop.CanWrite && cpa != null && m_Mobile.AccessLevel >= cpa.WriteLevel && !cpa.ReadOnly )
						AddButton( x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3, GumpButtonType.Reply, 0 );
				}
			}
		}

		public static string[] m_BoolNames = new string[]{ "True", "False" };
		public static object[] m_BoolValues = new object[]{ true, false };

		public static string[] m_PoisonNames = new string[]{ "None", "Lesser", "Regular", "Greater", "Deadly", "Lethal" };
		public static object[] m_PoisonValues = new object[]{ null, Poison.Lesser, Poison.Regular, Poison.Greater, Poison.Deadly, Poison.Lethal };

		public class StackEntry
		{
			public object m_Object;
			public PropertyInfo m_Property;

			public StackEntry( object obj, PropertyInfo prop )
			{
				m_Object = obj;
				m_Property = prop;
			}
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;

			if ( !BaseCommand.IsAccessible( from, m_Object ) )
			{
				from.SendMessage( "You may no longer access their properties." );
				return;
			}

			switch ( info.ButtonID )
			{
				case 0: // Closed
				{
					if ( m_Stack != null && m_Stack.Count > 0 )
					{
						StackEntry entry = (StackEntry)m_Stack.Pop();

						from.SendGump( new PropertiesGump( from, entry.m_Object, m_Stack, null ) );
					}

					break;
				}
				case 1: // Previous
				{
					if ( m_Page > 0 )
						from.SendGump( new PropertiesGump( from, m_Object, m_Stack, m_List, m_Page - 1 ) );

					break;
				}
				case 2: // Next
				{
					if ( (m_Page + 1) * EntryCount < m_List.Count )
						from.SendGump( new PropertiesGump( from, m_Object, m_Stack, m_List, m_Page + 1 ) );

					break;
				}
				default:
				{
					int index = (m_Page * EntryCount) + (info.ButtonID - 3);

					if ( index >= 0 && index < m_List.Count )
					{
						PropertyInfo prop = m_List[index] as PropertyInfo;

						if ( prop == null )
							return;

						CPA attr = GetCPA( prop );

						if ( !prop.CanWrite || attr == null || from.AccessLevel < attr.WriteLevel || attr.ReadOnly )
							return;

						Type type = prop.PropertyType;

						if ( IsType( type, typeofMobile ) || IsType( type, typeofItem ) )
							from.SendGump( new SetObjectGump( prop, from, m_Object, m_Stack, type, m_Page, m_List ) );
						else if ( IsType( type, typeofType ) )
							from.Target = new SetObjectTarget( prop, from, m_Object, m_Stack, type, m_Page, m_List );
						else if ( IsType( type, typeofPoint3D ) )
							from.SendGump( new SetPoint3DGump( prop, from, m_Object, m_Stack, m_Page, m_List ) );
						else if ( IsType( type, typeofPoint2D ) )
							from.SendGump( new SetPoint2DGump( prop, from, m_Object, m_Stack, m_Page, m_List ) );
						else if ( IsType( type, typeofTimeSpan ) )
							from.SendGump( new SetTimeSpanGump( prop, from, m_Object, m_Stack, m_Page, m_List ) );
						else if ( IsCustomEnum( type ) )
							from.SendGump( new SetCustomEnumGump( prop, from, m_Object, m_Stack, m_Page, m_List, GetCustomEnumNames( type ) ) );
						else if ( IsType( type, typeofEnum ) )
							from.SendGump( new SetListOptionGump( prop, from, m_Object, m_Stack, m_Page, m_List, Enum.GetNames( type ), GetObjects( Enum.GetValues( type ) ) ) );
						else if ( IsType( type, typeofBool ) )
							from.SendGump( new SetListOptionGump( prop, from, m_Object, m_Stack, m_Page, m_List, m_BoolNames, m_BoolValues ) );
						else if ( IsType( type, typeofString ) || IsType( type, typeofReal ) || IsType( type, typeofNumeric ) )
							from.SendGump( new SetGump( prop, from, m_Object, m_Stack, m_Page, m_List ) );
						else if ( IsType( type, typeofPoison ) )
							from.SendGump( new SetListOptionGump( prop, from, m_Object, m_Stack, m_Page, m_List, m_PoisonNames, m_PoisonValues ) );
						else if ( IsType( type, typeofMap ) )
							from.SendGump( new SetListOptionGump( prop, from, m_Object, m_Stack, m_Page, m_List, Map.GetMapNames(), Map.GetMapValues() ) );
						else if ( IsType( type, typeofSkills ) && m_Object is Mobile )
						{
							from.SendGump( new PropertiesGump( from, m_Object, m_Stack, m_List, m_Page ) );
							from.SendGump( new SkillsGump( from, (Mobile)m_Object ) );
						}
						else if( HasAttribute( type, typeofPropertyObject, true ) )
						{
							object obj = prop.GetValue( m_Object, null );

							if ( obj != null )
								from.SendGump( new PropertiesGump( from, obj, m_Stack, new StackEntry( m_Object, prop ) ) );
							else
								from.SendGump( new PropertiesGump( from, m_Object, m_Stack, m_List, m_Page ) );
						}
					}

					break;
				}
			}
		}

		private static object[] GetObjects( Array a )
		{
			object[] list = new object[a.Length];

			for ( int i = 0; i < list.Length; ++i )
				list[i] = a.GetValue( i );

			return list;
		}

		private static bool IsCustomEnum( Type type )
		{
			return type.IsDefined( typeofCustomEnum, false );
		}

		public static void OnValueChanged( object obj, PropertyInfo prop, Stack stack )
		{
			if ( stack == null || stack.Count == 0 )
				return;

			if ( !prop.PropertyType.IsValueType )
				return;

			StackEntry peek = (StackEntry)stack.Peek();

			if ( peek.m_Property.CanWrite )
				peek.m_Property.SetValue( peek.m_Object, obj, null );
		}

		private static string[] GetCustomEnumNames( Type type )
		{
			object[] attrs = type.GetCustomAttributes( typeofCustomEnum, false );

			if ( attrs.Length == 0 )
				return new string[0];

			CustomEnumAttribute ce = attrs[0] as CustomEnumAttribute;

			if ( ce == null )
				return new string[0];

			return ce.Names;
		}

		private static bool HasAttribute( Type type, Type check, bool inherit )
		{
			object[] objs = type.GetCustomAttributes( check, inherit );

			return ( objs != null && objs.Length > 0 );
		}

		private static bool IsType( Type type, Type check )
		{
			return type == check || type.IsSubclassOf( check );
		}

		private static bool IsType( Type type, Type[] check )
		{
			for ( int i = 0; i < check.Length; ++i )
				if ( IsType( type, check[i] ) )
					return true;

			return false;
		}

		private static Type typeofMobile = typeof( Mobile );
		private static Type typeofItem = typeof( Item );
		private static Type typeofType = typeof( Type );
		private static Type typeofPoint3D = typeof( Point3D );
		private static Type typeofPoint2D = typeof( Point2D );
		private static Type typeofTimeSpan = typeof( TimeSpan );
		private static Type typeofCustomEnum = typeof( CustomEnumAttribute );
		private static Type typeofEnum = typeof( Enum );
		private static Type typeofBool = typeof( Boolean );
		private static Type typeofString = typeof( String );
		private static Type typeofPoison = typeof( Poison );
		private static Type typeofMap = typeof( Map );
		private static Type typeofSkills = typeof( Skills );
		private static Type typeofPropertyObject = typeof( PropertyObjectAttribute );
		private static Type typeofNoSort = typeof( NoSortAttribute );

		private static Type[] typeofReal = new Type[]
			{
				typeof( Single ),
				typeof( Double )
			};

		private static Type[] typeofNumeric = new Type[]
			{
				typeof( Byte ),
				typeof( Int16 ),
				typeof( Int32 ),
				typeof( Int64 ),
				typeof( SByte ),
				typeof( UInt16 ),
				typeof( UInt32 ),
				typeof( UInt64 )
			};

		private string ValueToString( PropertyInfo prop )
		{
			return ValueToString( m_Object, prop );
		}

		public static string ValueToString( object obj, PropertyInfo prop )
		{
			try
			{
				return ValueToString( prop.GetValue( obj, null ) );
			}
			catch ( Exception e )
			{
				return String.Format( "!{0}!", e.GetType() );
			}
		}

		public static string ValueToString( object o )
		{
			if ( o == null )
			{
				return "-null-";
			}
			else if ( o is string )
			{
				return String.Format( "\"{0}\"", (string)o );
			}
			else if ( o is bool )
			{
				return o.ToString();
			}
			else if ( o is char )
			{
				return String.Format( "0x{0:X} '{1}'", (int)(char)o, (char)o );
			}
			else if ( o is Serial )
			{
				Serial s = (Serial)o;

				if ( s.IsValid )
				{
					if ( s.IsItem )
					{
						return String.Format( "(I) 0x{0:X}", s.Value );
					}
					else if ( s.IsMobile )
					{
						return String.Format( "(M) 0x{0:X}", s.Value );
					}
				}

				return String.Format( "(?) 0x{0:X}", s.Value );
			}
			else if ( o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong )
			{
				return String.Format( "{0} (0x{0:X})", o );
			}
			else if ( o is Mobile )
			{
				return String.Format( "(M) 0x{0:X} \"{1}\"", ((Mobile)o).Serial.Value, ((Mobile)o).Name );
			}
			else if ( o is Item )
			{
				return String.Format( "(I) 0x{0:X}", ((Item)o).Serial );
			}
			else if ( o is Type )
			{
				return ((Type)o).Name;
			}
			else
			{
				return o.ToString();
			}
		}

		private ArrayList BuildList()
		{
			Type type = m_Object.GetType();

			PropertyInfo[] props = type.GetProperties( BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public );

			ArrayList groups = GetGroups( type, props );
			ArrayList list = new ArrayList();

			for ( int i = 0; i < groups.Count; ++i )
			{
				DictionaryEntry de = (DictionaryEntry)groups[i];
				ArrayList groupList = (ArrayList)de.Value;

				if ( !HasAttribute( (Type)de.Key, typeofNoSort, false ) )
					groupList.Sort( PropertySorter.Instance );

				if ( i != 0 )
					list.Add( null );

				list.Add( de.Key );
				list.AddRange( groupList );
			}

			return list;
		}

		private static Type typeofCPA = typeof( CPA );
		private static Type typeofObject = typeof( object );

		private static CPA GetCPA( PropertyInfo prop )
		{
			object[] attrs = prop.GetCustomAttributes( typeofCPA, false );

			if ( attrs.Length > 0 )
				return attrs[0] as CPA;
			else
				return null;
		}

		private ArrayList GetGroups( Type objectType, PropertyInfo[] props )
		{
			Hashtable groups = new Hashtable();

			for ( int i = 0; i < props.Length; ++i )
			{
				PropertyInfo prop = props[i];

				if ( prop.CanRead )
				{
					CPA attr = GetCPA( prop );

					if ( attr != null && m_Mobile.AccessLevel >= attr.ReadLevel )
					{
						Type type = prop.DeclaringType;

						while ( true )
						{
							Type baseType = type.BaseType;

							if ( baseType == null || baseType == typeofObject )
								break;

							if ( baseType.GetProperty( prop.Name, prop.PropertyType ) != null )
								type = baseType;
							else
								break;
						}

						ArrayList list = (ArrayList)groups[type];

						if ( list == null )
							groups[type] = list = new ArrayList();

						list.Add( prop );
					}
				}
			}

			ArrayList sorted = new ArrayList( groups );

			sorted.Sort( new GroupComparer( objectType ) );

			return sorted;
		}

		public static object GetObjectFromString( Type t, string s )
		{
			if ( t == typeof( string ) )
			{
				return s;
			}
			else if ( t == typeof( byte ) || t == typeof( sbyte ) || t == typeof( short ) || t == typeof( ushort ) || t == typeof( int ) || t == typeof( uint ) || t == typeof( long ) || t == typeof( ulong ) )
			{
				if ( s.StartsWith( "0x" ) )
				{
					if ( t == typeof( ulong ) || t == typeof( uint ) || t == typeof( ushort ) || t == typeof( byte ) )
					{
						return Convert.ChangeType( Convert.ToUInt64( s.Substring( 2 ), 16 ), t );
					}
					else
					{
						return Convert.ChangeType( Convert.ToInt64( s.Substring( 2 ), 16 ), t );
					}
				}
				else
				{
					return Convert.ChangeType( s, t );
				}
			}
			else if ( t == typeof( double ) || t == typeof( float ) )
			{
				return Convert.ChangeType( s, t );
			}
			else if ( t.IsDefined( typeof( ParsableAttribute ), false ) )
			{
				MethodInfo parseMethod = t.GetMethod( "Parse", new Type[]{ typeof( string ) } );

				return parseMethod.Invoke( null, new object[]{ s } );
			}

			throw new Exception( "bad" );
		}

		private static string GetStringFromObject( object o )
		{
			if ( o == null )
			{
				return "-null-";
			}
			else if ( o is string )
			{
				return String.Format( "\"{0}\"", (string)o );
			}
			else if ( o is bool )
			{
				return o.ToString();
			}
			else if ( o is char )
			{
				return String.Format( "0x{0:X} '{1}'", (int)(char)o, (char)o );
			}
			else if ( o is Serial )
			{
				Serial s = (Serial)o;

				if ( s.IsValid )
				{
					if ( s.IsItem )
					{
						return String.Format( "(I) 0x{0:X}", s.Value );
					}
					else if ( s.IsMobile )
					{
						return String.Format( "(M) 0x{0:X}", s.Value );
					}
				}

				return String.Format( "(?) 0x{0:X}", s.Value );
			}
			else if ( o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong )
			{
				return String.Format( "{0} (0x{0:X})", o );
			}
			else if ( o is Mobile )
			{
				return String.Format( "(M) 0x{0:X} \"{1}\"", ((Mobile)o).Serial.Value, ((Mobile)o).Name );
			}
			else if ( o is Item )
			{
				return String.Format( "(I) 0x{0:X}", ((Item)o).Serial );
			}
			else if ( o is Type )
			{
				return ((Type)o).Name;
			}
			else
			{
				return o.ToString();
			}
		}

		private class PropertySorter : IComparer
		{
			public static readonly PropertySorter Instance = new PropertySorter();

			private PropertySorter()
			{
			}

			public int Compare( object x, object y )
			{
				if ( x == null && y == null )
					return 0;
				else if ( x == null )
					return -1;
				else if ( y == null )
					return 1;

				PropertyInfo a = x as PropertyInfo;
				PropertyInfo b = y as PropertyInfo;

				if ( a == null || b == null )
					throw new ArgumentException();

				return a.Name.CompareTo( b.Name );
			}
		}

		private class GroupComparer : IComparer
		{
			private Type m_Start;

			public GroupComparer( Type start )
			{
				m_Start = start;
			}

			private static Type typeofObject = typeof( Object );

			private int GetDistance( Type type )
			{
				Type current = m_Start;

				int dist;

				for ( dist = 0; current != null && current != typeofObject && current != type; ++dist )
					current = current.BaseType;

				return dist;
			}

			public int Compare( object x, object y )
			{
				if ( x == null && y == null )
					return 0;
				else if ( x == null )
					return -1;
				else if ( y == null )
					return 1;

				if ( !(x is DictionaryEntry) || !(y is DictionaryEntry) )
					throw new ArgumentException();

				DictionaryEntry de1 = (DictionaryEntry)x;
				DictionaryEntry de2 = (DictionaryEntry)y;

				Type a = (Type)de1.Key;
				Type b = (Type)de2.Key;

				return GetDistance( a ).CompareTo( GetDistance( b ) );
			}
		}
	}
}