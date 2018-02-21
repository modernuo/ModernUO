using System;

namespace Server.Items
{
	[FlippableAttribute( 0x1BD7, 0x1BDA )]
	public class Board : Item, ICommodity
	{
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set { m_Resource = value; InvalidateProperties(); }
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

		bool ICommodity.IsDeedable { get { return true; } }

		[Constructible]
		public Board()
			: this( 1 )
		{
		}

		[Constructible]
		public Board( int amount )
			: this( CraftResource.RegularWood, amount )
		{
		}

		public Board( Serial serial )
			: base( serial )
		{
		}

		[Constructible]
		public Board( CraftResource resource ) : this( resource, 1 )
		{
		}

		[Constructible]
		public Board( CraftResource resource, int amount )
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

			writer.Write( (int) 3 );

			writer.Write( (int)m_Resource );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 3:
				case 2:
					{
						m_Resource = (CraftResource)reader.ReadInt();
						break;
					}
			}

			if ( (version == 0 && Weight == 0.1) || ( version <= 2 && Weight == 2 ) )
				Weight = -1;

			if ( version <= 1 )
				m_Resource = CraftResource.RegularWood;
		}
	}


	public class HeartwoodBoard : Board
	{
		[Constructible]
		public HeartwoodBoard()
			: this( 1 )
		{
		}

		[Constructible]
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

	public class BloodwoodBoard : Board
	{
		[Constructible]
		public BloodwoodBoard()
			: this( 1 )
		{
		}

		[Constructible]
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

	public class FrostwoodBoard : Board
	{
		[Constructible]
		public FrostwoodBoard()
			: this( 1 )
		{
		}

		[Constructible]
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

	public class OakBoard : Board
	{
		[Constructible]
		public OakBoard()
			: this( 1 )
		{
		}

		[Constructible]
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

	public class AshBoard : Board
	{
		[Constructible]
		public AshBoard()
			: this( 1 )
		{
		}

		[Constructible]
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

	public class YewBoard : Board
	{
		[Constructible]
		public YewBoard()
			: this( 1 )
		{
		}

		[Constructible]
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