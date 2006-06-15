using System;
using System.Collections;
using System.IO;
using System.Xml;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Regions
{
	public abstract class SpawnDefinition
	{
		protected SpawnDefinition()
		{
		}

		public abstract object Spawn( SpawnEntry entry );

		public abstract bool CanSpawn( params Type[] types );

		public static SpawnDefinition GetSpawnDefinition( XmlElement xml )
		{
			switch ( xml.Name )
			{
				case "object":
				{
					Type type = null;
					if ( !Region.ReadType( xml, "type", ref type ) )
						return null;

					if ( typeof( Mobile ).IsAssignableFrom( type ) )
					{
						return SpawnMobile.Get( type );
					}
					else if ( typeof( Item ).IsAssignableFrom( type ) )
					{
						return SpawnItem.Get( type );
					}
					else
					{
						Console.WriteLine( "Invalid type '{0}' in a SpawnDefinition", type.FullName );
						return null;
					}
				}
				case "group":
				{
					string group = null;
					if ( !Region.ReadString( xml, "name", ref group ) )
						return null;

					SpawnDefinition def = (SpawnDefinition) SpawnGroup.Table[group];

					if ( def == null )
					{
						Console.WriteLine( "Could not find group '{0}' in a SpawnDefinition", group );
						return null;
					}
					else
					{
						return def;
					}
				}
				case "treasureChest":
				{
					int itemID = 0xE43;
					Region.ReadInt32( xml, "itemID", ref itemID, false );

					object oLevel = BaseTreasureChest.TreasureLevel.Level2;
					Region.ReadEnum( xml, "level", typeof( BaseTreasureChest.TreasureLevel ), ref oLevel, false );

					return new SpawnTreasureChest( itemID, (BaseTreasureChest.TreasureLevel) oLevel );
				}
				default:
				{
					return null;
				}
			}
		}
	}

	public abstract class SpawnType : SpawnDefinition
	{
		private Type m_Type;
		private bool m_Init;

		public Type Type{ get{ return m_Type; } }

		public abstract int Height{ get; }
		public abstract bool Land{ get; }
		public abstract bool Water{ get; }

		protected SpawnType( Type type )
		{
			m_Type = type;
			m_Init = false;
		}

		protected void EnsureInit()
		{
			if ( m_Init )
				return;

			Init();
			m_Init = true;
		}

		protected virtual void Init()
		{
		}

		public override object Spawn( SpawnEntry entry )
		{
			BaseRegion region = entry.Region;
			Map map = region.Map;

			Point3D loc = entry.RandomSpawnLocation( this.Height, this.Land, this.Water );
			if ( loc == Point3D.Zero )
				return null;

			return Construct( entry, loc, map );
		}

		protected abstract object Construct( SpawnEntry entry, Point3D loc, Map map );

		public override bool CanSpawn( params Type[] types )
		{
			for ( int i = 0; i < types.Length; i++ )
			{
				if ( types[i] == m_Type )
					return true;
			}

			return false;
		}
	}

	public class SpawnMobile : SpawnType
	{
		private static Hashtable m_Table = new Hashtable();

		public static SpawnMobile Get( Type type )
		{
			SpawnMobile sm = (SpawnMobile) m_Table[type];

			if ( sm == null )
			{
				sm = new SpawnMobile( type );
				m_Table[type] = sm;
			}

			return sm;
		}

		protected bool m_Land;
		protected bool m_Water;

		public override int Height{ get{ return 16; } }
		public override bool Land{ get{ EnsureInit(); return m_Land; } }
		public override bool Water{ get{ EnsureInit(); return m_Water; } }

		protected SpawnMobile( Type type ) : base( type )
		{
		}

		protected override void Init()
		{
			Mobile mob = (Mobile) Activator.CreateInstance( Type );

			m_Land = !mob.CantWalk;
			m_Water = mob.CanSwim;

			mob.Delete();
		}

		protected override object Construct( SpawnEntry entry, Point3D loc, Map map )
		{
			Mobile mobile = CreateMobile();

			BaseCreature creature = mobile as BaseCreature;
			if ( creature != null )
			{
				creature.Home = entry.Home;
				creature.RangeHome = entry.Range;
			}

			if ( entry.Direction != SpawnEntry.InvalidDirection )
				mobile.Direction = entry.Direction;

			mobile.OnBeforeSpawn( loc, map );
			mobile.MoveToWorld( loc, map );
			mobile.OnAfterSpawn();

			return mobile;
		}

		protected virtual Mobile CreateMobile()
		{
			return (Mobile) Activator.CreateInstance( Type );
		}
	}

	public class SpawnItem : SpawnType
	{
		private static Hashtable m_Table = new Hashtable();

		public static SpawnItem Get( Type type )
		{
			SpawnItem si = (SpawnItem) m_Table[type];

			if ( si == null )
			{
				si = new SpawnItem( type );
				m_Table[type] = si;
			}

			return si;
		}

		protected int m_Height;

		public override int Height{ get{ EnsureInit(); return m_Height; } }
		public override bool Land{ get{ return true; } }
		public override bool Water{ get{ return false; } }

		protected SpawnItem( Type type ) : base( type )
		{
		}

		protected override void Init()
		{
			Item item = (Item) Activator.CreateInstance( Type );

			m_Height = item.ItemData.Height;

			item.Delete();
		}

		protected override object Construct( SpawnEntry entry, Point3D loc, Map map )
		{
			Item item = CreateItem();

			item.OnBeforeSpawn( loc, map );
			item.MoveToWorld( loc, map );
			item.OnAfterSpawn();

			return item;
		}

		protected virtual Item CreateItem()
		{
			return (Item) Activator.CreateInstance( Type );
		}
	}

	public class SpawnTreasureChest : SpawnItem
	{
		private int m_ItemID;
		private BaseTreasureChest.TreasureLevel m_Level;

		public int ItemID{ get{ return m_ItemID; } }
		public BaseTreasureChest.TreasureLevel Level{ get{ return m_Level; } }

		public SpawnTreasureChest( int itemID, BaseTreasureChest.TreasureLevel level ) : base( typeof( BaseTreasureChest ) )
		{
			m_ItemID = itemID;
			m_Level = level;
		}

		protected override void Init()
		{
			m_Height = TileData.ItemTable[m_ItemID & 0x3FFF].Height;
		}

		protected override Item CreateItem()
		{
			return new BaseTreasureChest( m_ItemID, m_Level );
		}
	}

	public class SpawnGroupElement
	{
		private SpawnDefinition m_SpawnDefinition;
		private int m_Weight;

		public SpawnDefinition SpawnDefinition{ get{ return m_SpawnDefinition; } }
		public int Weight{ get{ return m_Weight; } }

		public SpawnGroupElement( SpawnDefinition spawnDefinition, int weight )
		{
			m_SpawnDefinition = spawnDefinition;
			m_Weight = weight;
		}
	}

	public class SpawnGroup : SpawnDefinition
	{
		private static Hashtable m_Table = new Hashtable();

		public static Hashtable Table{ get{ return m_Table; } }

		public static void Register( SpawnGroup group )
		{
			if ( m_Table.Contains( group.Name ) )
				Console.WriteLine( "Warning: Double SpawnGroup name '{0}'", group.Name );
			else
				m_Table[group.Name] = group;
		}

		static SpawnGroup()
		{
			string path = Path.Combine( Core.BaseDirectory, "Data/SpawnDefinitions.xml" );
			if ( !File.Exists( path ) )
				return;

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load( path );

				XmlElement root = doc["spawnDefinitions"];
				if ( root == null )
					return;

				foreach ( XmlElement xmlDef in root.SelectNodes( "spawnGroup" ) )
				{
					string name = null;
					if ( !Region.ReadString( xmlDef, "name", ref name ) )
						continue;

					ArrayList list = new ArrayList();
					foreach ( XmlNode node in xmlDef.ChildNodes )
					{
						XmlElement el = node as XmlElement;

						if ( el != null )
						{
							SpawnDefinition def = GetSpawnDefinition( el );
							if ( def == null )
								continue;

							int weight = 1;
							Region.ReadInt32( el, "weight", ref weight, false );

							SpawnGroupElement groupElement = new SpawnGroupElement( def, weight );
							list.Add( groupElement );
						}
					}

					SpawnGroupElement[] elements = (SpawnGroupElement[]) list.ToArray( typeof( SpawnGroupElement ) );
					SpawnGroup group = new SpawnGroup( name, elements );
					Register( group );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Could not load SpawnDefinitions.xml: " + ex.Message );
			}
		}

		private string m_Name;
		private SpawnGroupElement[] m_Elements;
		private int m_TotalWeight;

		public string Name{ get{ return m_Name; } }
		public SpawnGroupElement[] Elements{ get{ return m_Elements; } }

		public SpawnGroup( string name, SpawnGroupElement[] elements )
		{
			m_Name = name;
			m_Elements = elements;

			m_TotalWeight = 0;
			for ( int i = 0; i < elements.Length; i++ )
				m_TotalWeight += elements[i].Weight;
		}

		public override object Spawn( SpawnEntry entry )
		{
			int index = Utility.Random( m_TotalWeight );

			for ( int i = 0; i < m_Elements.Length; i++ )
			{
				SpawnGroupElement element = m_Elements[i];

				if ( index < element.Weight )
					return element.SpawnDefinition.Spawn( entry );

				index -= element.Weight;
			}

			return null;
		}

		public override bool CanSpawn( params Type[] types )
		{
			for ( int i = 0; i < m_Elements.Length; i++ )
			{
				if ( m_Elements[i].SpawnDefinition.CanSpawn( types ) )
					return true;
			}

			return false;
		}
	}
}