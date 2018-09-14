using System;
using System.Collections;
using System.IO;
using System.Xml;
using Server.Mobiles;
using Server.Items;
using System.Collections.Generic;

namespace Server.Regions
{
	public abstract class SpawnDefinition
	{
		protected SpawnDefinition()
		{
		}

		public abstract ISpawnable Spawn( SpawnEntry entry );

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

					if ( typeof( Item ).IsAssignableFrom( type ) )
					{
						return SpawnItem.Get( type );
					}
					Console.WriteLine( "Invalid type '{0}' in a SpawnDefinition", type.FullName );
					return null;
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

					return def;
				}
				case "treasureChest":
				{
					int itemID = 0xE43;
					Region.ReadInt32( xml, "itemID", ref itemID, false );

					BaseTreasureChest.TreasureLevel level = BaseTreasureChest.TreasureLevel.Level2;

					Region.ReadEnum( xml, "level", ref level, false );

					return new SpawnTreasureChest( itemID, level );
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
		private bool m_Init;

		public Type Type { get; }

		public abstract int Height{ get; }
		public abstract bool Land{ get; }
		public abstract bool Water{ get; }

		protected SpawnType( Type type )
		{
			Type = type;
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

		public override ISpawnable Spawn( SpawnEntry entry )
		{
			Region region = entry.Region;
			Map map = region.Map;

			Point3D loc = entry.RandomSpawnLocation( Height, Land, Water );

			if ( loc == Point3D.Zero )
				return null;

			return Construct( entry, loc, map );
		}

		protected abstract ISpawnable Construct( SpawnEntry entry, Point3D loc, Map map );

		public override bool CanSpawn( params Type[] types )
		{
			for ( int i = 0; i < types.Length; i++ )
			{
				if ( types[i] == Type )
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

		public override int Height => 16;
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

		protected override ISpawnable Construct(SpawnEntry entry, Point3D loc, Map map)
		{
			Mobile mobile = CreateMobile();

			BaseCreature creature = mobile as BaseCreature;

			if ( creature != null )
			{
				creature.Home = entry.HomeLocation;
				creature.HomeMap = map;
				creature.RangeHome = entry.HomeRange;
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
		public override bool Land => true;
		public override bool Water => false;

		protected SpawnItem( Type type ) : base( type )
		{
		}

		protected override void Init()
		{
			Item item = (Item) Activator.CreateInstance( Type );

			m_Height = item.ItemData.Height;

			item.Delete();
		}

		protected override ISpawnable Construct( SpawnEntry entry, Point3D loc, Map map )
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
		public int ItemID { get; }

		public BaseTreasureChest.TreasureLevel Level { get; }

		public SpawnTreasureChest( int itemID, BaseTreasureChest.TreasureLevel level ) : base( typeof( BaseTreasureChest ) )
		{
			ItemID = itemID;
			Level = level;
		}

		protected override void Init()
		{
			m_Height = TileData.ItemTable[ItemID & TileData.MaxItemValue].Height;
		}

		protected override Item CreateItem()
		{
			return new BaseTreasureChest( ItemID, Level );
		}
	}

	public class SpawnGroupElement
	{
		public SpawnDefinition SpawnDefinition { get; }

		public int Weight { get; }

		public SpawnGroupElement( SpawnDefinition spawnDefinition, int weight )
		{
			SpawnDefinition = spawnDefinition;
			Weight = weight;
		}
	}

	public class SpawnGroup : SpawnDefinition
	{
		public static Hashtable Table { get; } = new Hashtable();

		public static void Register( SpawnGroup group )
		{
			if ( Table.Contains( group.Name ) )
				Console.WriteLine( "Warning: Double SpawnGroup name '{0}'", group.Name );
			else
				Table[group.Name] = group;
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

					List<SpawnGroupElement> list = new List<SpawnGroupElement>();
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

					SpawnGroupElement[] elements = list.ToArray();
					SpawnGroup group = new SpawnGroup( name, elements );
					Register( group );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Could not load SpawnDefinitions.xml: " + ex.Message );
			}
		}

		private int m_TotalWeight;

		public string Name { get; }

		public SpawnGroupElement[] Elements { get; }

		public SpawnGroup( string name, SpawnGroupElement[] elements )
		{
			Name = name;
			Elements = elements;

			m_TotalWeight = 0;
			for ( int i = 0; i < elements.Length; i++ )
				m_TotalWeight += elements[i].Weight;
		}

		public override ISpawnable Spawn(SpawnEntry entry)
		{
			int index = Utility.Random( m_TotalWeight );

			for ( int i = 0; i < Elements.Length; i++ )
			{
				SpawnGroupElement element = Elements[i];

				if ( index < element.Weight )
					return element.SpawnDefinition.Spawn( entry );

				index -= element.Weight;
			}

			return null;
		}

		public override bool CanSpawn( params Type[] types )
		{
			for ( int i = 0; i < Elements.Length; i++ )
			{
				if ( Elements[i].SpawnDefinition.CanSpawn( types ) )
					return true;
			}

			return false;
		}
	}
}
