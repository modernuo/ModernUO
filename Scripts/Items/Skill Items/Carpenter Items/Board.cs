using System;

namespace Server.Items
{
	[FlipableAttribute( 0x1BD7, 0x1BDA )]
	public abstract class BaseBoard : Item, ICommodity
	{
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set { m_Resource = value; InvalidateProperties(); }
		}
		
		#region Old Board Serialization Vars
		/* DO NOT USE! Only used in serialization of boards that originally derived from Board */
		private bool m_InheritsItem;
		private int m_OldVersion;
		
		protected bool InheritsItem
		{
			get{ return m_InheritsItem; }
		}
		
		protected int OldVersion
		{
			get{ return m_OldVersion; }
		}
		#endregion

		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} {1} board" : "{0} {1} boards", Amount, CraftResources.IsStandard( m_Resource ) ? String.Empty : CraftResources.GetName( m_Resource ).ToLower() );
			}
		}

		int ICommodity.DescriptionNumber 
		{ 
			get
			{
				if ( m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.YewWood )
					return 1075052 + ( (int)m_Resource - (int)CraftResource.OakWood );

				switch ( m_Resource )
				{
					case CraftResource.Bloodwood: return 1075055;
					case CraftResource.Frostwood: return 1075056;
					case CraftResource.Heartwood: return 1075062;	//WHY Osi.  Why?
				}

				return LabelNumber;
			} 
		}

		public BaseBoard( Serial serial )
			: base( serial )
		{
		}

		public BaseBoard( CraftResource resource, int amount )
			: base( 0x1BD7 )
		{
			Stackable = true;
			Amount = amount;

			m_Resource = resource;
			Hue = CraftResources.GetHue( resource );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( !CraftResources.IsStandard( m_Resource ) )
			{
				int num = CraftResources.GetLocalizationNumber( m_Resource );

				if ( num > 0 )
					list.Add( num );
				else
					list.Add( CraftResources.GetName( m_Resource ) );
			}
		}

		

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 4 );

			writer.Write( (int)m_Resource );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 4:
				{
					m_Resource = (CraftResource)reader.ReadInt();
					break;
				}
				/** Below is for deserialization of old boards that orignally inherited from Board or Item **/
				case 3: // For all boards
				case 2:
				{
					m_Resource = (CraftResource)reader.ReadInt();
					goto case 0;
				}
				case 1: // For old standard boards
				case 0: 
				{
					m_InheritsItem = true;
					m_OldVersion = version;
					break;
				}
			}

			if ( (version == 0 && Weight == 0.1) || ( version <= 2 && Weight == 2 ) )
				Weight = -1;
		}
	}
	
	public class Board : BaseBoard
	{
		[Constructable]
		public Board()
			: this( 1 )
		{
		}

		[Constructable]
		public Board( int amount )
			: base( CraftResource.RegularWood, amount )
		{
		}

		public Board( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 4 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); // Required for BaseBoard insertion

			switch ( version )
			{
				/** Versions 0 through 3 originally inherited from Item. Versions 0 and 1 are from before Board became a CraftResource. **/
				case 1:
				case 0:
				{
					Resource = CraftResource.RegularWood;
					break;
				}
			}	
		}
	}


	public class HeartwoodBoard : BaseBoard
	{
		[Constructable]
		public HeartwoodBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public HeartwoodBoard( int amount )
			: base( CraftResource.Heartwood, amount )
		{
		}

		public HeartwoodBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class BloodwoodBoard : BaseBoard
	{
		[Constructable]
		public BloodwoodBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public BloodwoodBoard( int amount )
			: base( CraftResource.Bloodwood, amount )
		{
		}

		public BloodwoodBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class FrostwoodBoard : BaseBoard
	{
		[Constructable]
		public FrostwoodBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public FrostwoodBoard( int amount )
			: base( CraftResource.Frostwood, amount )
		{
		}

		public FrostwoodBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class OakBoard : BaseBoard
	{
		[Constructable]
		public OakBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public OakBoard( int amount )
			: base( CraftResource.OakWood, amount )
		{
		}

		public OakBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class AshBoard : BaseBoard
	{
		[Constructable]
		public AshBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public AshBoard( int amount )
			: base( CraftResource.AshWood, amount )
		{
		}

		public AshBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class YewBoard : BaseBoard
	{
		[Constructable]
		public YewBoard()
			: this( 1 )
		{
		}

		[Constructable]
		public YewBoard( int amount )
			: base( CraftResource.YewWood, amount )
		{
		}

		public YewBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}