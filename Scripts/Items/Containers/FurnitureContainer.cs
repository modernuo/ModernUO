using System;
using System.Collections.Generic;
using Server;
using Server.Misc;
using Server.Multis;
using Server.Network;
using Server.Engines.Craft;

namespace Server.Items
{
	public abstract class BaseFurnitureContainer : BaseContainer, ICraftable
	{
		private Mobile m_Crafter;
		private CraftQuality m_Quality;
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Crafter
		{
			get { return m_Crafter; } 
			set { m_Crafter = value; InvalidateProperties(); } 
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CraftQuality Quality
		{ 
			get { return m_Quality; } 
			set { m_Quality = value; InvalidateProperties(); } 
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set
			{
				if ( m_Resource != value )
				{
					m_Resource = value;
					Hue = CraftResources.GetHue( m_Resource );
					InvalidateProperties();
				}
			}
		}
		
		public virtual CraftResource DefaultResource{ get{ return CraftResource.RegularWood; } }
		public virtual bool DisplaysResource{ get{ return true; } }
		public virtual bool DisplaysMakersMark{ get{ return true; } }
		
		#region Old Container Serialization Vars
		/* DO NOT USE! Only used in serialization of furniture containers that originally derived from BaseContainer */
		private bool m_InheritsBaseCont;
		private int m_OldVersion;
		
		protected bool InheritsBaseCont
		{
			get{ return m_InheritsBaseCont; }
		}
		
		protected int OldVersion
		{
			get{ return m_OldVersion; }
		}
		#endregion
		
		public BaseFurnitureContainer( int itemID ) : base( itemID )
		{
			m_Quality = CraftQuality.Regular;
			Resource = DefaultResource;
		}

		public BaseFurnitureContainer(Serial serial) : base(serial)
		{
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Crafter != null && DisplaysMakersMark )
				list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

			if ( m_Quality == CraftQuality.Exceptional )
				list.Add( 1060636 ); // exceptional

			if( ( m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood ) && Hue == CraftResources.GetHue( m_Resource ) && DisplaysResource )
				list.Add( CraftResources.GetLocalizationNumber( m_Resource ) ); // resource name
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 2);
			
			writer.Write( m_Crafter );
			writer.WriteEncodedInt( (int) m_Quality );
			writer.WriteEncodedInt( (int) m_Resource );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			
			int version = reader.ReadInt();
			
			switch ( version )
			{
				case 2: 
				{
					m_Crafter = reader.ReadMobile();
					m_Quality = (CraftQuality)reader.ReadEncodedInt();
					m_Resource = (CraftResource)reader.ReadEncodedInt();
					break;
				}
				case 1: //Captcha for EmptyBookcase
				case 0: 
				{
					m_InheritsBaseCont = true;
					m_OldVersion = version;
					m_Quality = CraftQuality.Regular;
					m_Resource = DefaultResource;
					break;
				}
			}
		}
		
		public virtual int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			if ( Core.ML )
			{
				Quality = (CraftQuality)quality;
			
				if ( makersMark )
					Crafter = from;

				Type resourceType = typeRes;

				if ( resourceType == null )
					resourceType = craftItem.Resources.GetAt( 0 ).ItemType;

				Resource = CraftResources.GetFromType( resourceType );

				CraftContext context = craftSystem.GetContext( from );

				if ( context != null && context.DoNotColor )
					Hue = 0;
			}
			
			return quality;
		}
	}
	
	[Furniture]
	[Flipable( 0x2815, 0x2816 )]
	public class TallCabinet : BaseFurnitureContainer
	{
		[Constructable]
		public TallCabinet() : base( 0x2816 )
		{
			Weight = 1.0;
		}

		public TallCabinet( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
		}
	}

	[Furniture]
	[Flipable( 0x2817, 0x2818 )]
	public class ShortCabinet : BaseFurnitureContainer
	{
		[Constructable]
		public ShortCabinet() : base( 0x2818 )
		{
			Weight = 1.0;
		}

		public ShortCabinet( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
		}
	}


	[Furniture]
	[Flipable( 0x2857, 0x2858 )]
	public class RedArmoire : BaseFurnitureContainer
	{
		[Constructable]
		public RedArmoire() : base( 0x2858 )
		{
			Weight = 10.0;
		}

		public RedArmoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
			
			if ( Weight == 1.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	[Flipable( 0x285D, 0x285E )]
	public class CherryArmoire : BaseFurnitureContainer
	{
		[Constructable]
		public CherryArmoire() : base( 0x285E )
		{
			Weight = 10.0;
		}

		public CherryArmoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
			
			if ( Weight == 1.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	[Flipable( 0x285B, 0x285C )]
	public class MapleArmoire : BaseFurnitureContainer
	{
		[Constructable]
		public MapleArmoire() : base( 0x285C )
		{
			Weight = 10.0;
		}

		public MapleArmoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
			
			if ( Weight == 1.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	[Flipable( 0x2859, 0x285A )]
	public class ElegantArmoire : BaseFurnitureContainer
	{
		[Constructable]
		public ElegantArmoire() : base( 0x285A )
		{
			Weight = 10.0;
		}

		public ElegantArmoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
			
			if ( Weight == 1.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	[Flipable( 0xA97, 0xA99, 0xA98, 0xA9A, 0xA9B, 0xA9C )]
	public class FullBookcase : BaseFurnitureContainer
	{
		[Constructable]
		public FullBookcase() : base( 0xA99 )
		{
			Weight = 1.0;
		}

		public FullBookcase( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
		}
	}

	[Furniture]
	[Flipable( 0xA9D, 0xA9E )]
	public class EmptyBookcase : BaseFurnitureContainer
	{
		[Constructable]
		public EmptyBookcase() : base( 0xA9E )
		{
		}

		public EmptyBookcase( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion

			if ( version == 0 && Weight == 1.0 )
				Weight = -1;
		}
	}

	[Furniture]
	[Flipable( 0xA2C, 0xA34 )]
	public class Drawer : BaseFurnitureContainer
	{
		[Constructable]
		public Drawer() : base( 0xA34 )
		{
			Weight = 1.0;
		}

		public Drawer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
		}
	}

	[Furniture]
	[Flipable( 0xA30, 0xA38 )]
	public class FancyDrawer : BaseFurnitureContainer
	{
		[Constructable]
		public FancyDrawer() : base( 0xA38 )
		{
			Weight = 1.0;
		}

		public FancyDrawer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion
		}
	}
	
	[Furniture]
	[Flipable( 0x2DF1, 0x2DF2 )]
	public class OrnateElvenChest : BaseFurnitureContainer
	{
		[Constructable]
		public OrnateElvenChest() : base( 0x2DF1 )
		{
			Weight = 1.0;
			GumpID = 0x10C;
		}

		public OrnateElvenChest( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			
			if ( version < 1 )
				GumpID = 0x10C; 
		}
	}
	
	[Furniture]
	[Flipable( 0x2DF3, 0x2DF4 )]
	public class OrnateElvenBox : BaseFurnitureContainer
	{
		[Constructable]
		public OrnateElvenBox() : base( 0x2DF3 )
		{
			Weight = 1.0;
			GumpID = 0x10C;
		}

		public OrnateElvenBox( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			
			if ( version < 1 )
				GumpID = 0x10C; 
		}
	}

	[Furniture]
	[Flipable( 0xA4F, 0xA53 )]
	public class Armoire : BaseFurnitureContainer
	{
		[Constructable]
		public Armoire() : base( 0xA53 )
		{
			Weight = 1.0;
		}

		public override void DisplayTo( Mobile m )
		{
			if ( DynamicFurniture.Open( this, m ) )
				base.DisplayTo( m );
		}

		public Armoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion

			DynamicFurniture.Close( this );
		}
	}

	[Furniture]
	[Flipable( 0xA4D, 0xA51 )]
	public class FancyArmoire : BaseFurnitureContainer
	{
		[Constructable]
		public FancyArmoire() : base( 0xA51 )
		{
			Weight = 1.0;
		}

		public override void DisplayTo( Mobile m )
		{
			if ( DynamicFurniture.Open( this, m ) )
				base.DisplayTo( m );
		}

		public FancyArmoire( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = ( InheritsBaseCont ? OldVersion : reader.ReadInt() ); //Required for BaseFurnitureContainer insertion

			DynamicFurniture.Close( this );
		}
	}

	public class DynamicFurniture
	{
		private static Dictionary<Container, Timer> m_Table = new Dictionary<Container, Timer>();

		public static bool Open( Container c, Mobile m )
		{
			if ( m_Table.ContainsKey( c ) )
			{
				c.SendRemovePacket();
				Close( c );
				c.Delta( ItemDelta.Update );
				c.ProcessDelta();
				return false;
			}

			if ( c is Armoire || c is FancyArmoire )
			{
				Timer t = new FurnitureTimer( c, m );
				t.Start();
				m_Table[c] = t;

				switch ( c.ItemID )
				{
					case 0xA4D: c.ItemID = 0xA4C; break;
					case 0xA4F: c.ItemID = 0xA4E; break;
					case 0xA51: c.ItemID = 0xA50; break;
					case 0xA53: c.ItemID = 0xA52; break;
				}
			}

			return true;
		}

		public static void Close( Container c )
		{
			Timer t = null;

			m_Table.TryGetValue( c, out t );

			if ( t != null )
			{
				t.Stop();
				m_Table.Remove( c );
			}

			if ( c is Armoire || c is FancyArmoire )
			{
				switch ( c.ItemID )
				{
					case 0xA4C: c.ItemID = 0xA4D; break;
					case 0xA4E: c.ItemID = 0xA4F; break;
					case 0xA50: c.ItemID = 0xA51; break;
					case 0xA52: c.ItemID = 0xA53; break;
				}
			}
		}
	}

	public class FurnitureTimer : Timer
	{
		private Container m_Container;
		private Mobile m_Mobile;

		public FurnitureTimer( Container c, Mobile m ) : base( TimeSpan.FromSeconds( 0.5 ), TimeSpan.FromSeconds( 0.5 ) )
		{
			Priority = TimerPriority.TwoFiftyMS;

			m_Container = c;
			m_Mobile = m;
		}

		protected override void OnTick()
		{
			if ( m_Mobile.Map != m_Container.Map || !m_Mobile.InRange( m_Container.GetWorldLocation(), 3 ) )
				DynamicFurniture.Close( m_Container );
		}
	}
}