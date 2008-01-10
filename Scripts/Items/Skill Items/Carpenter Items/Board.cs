using System;

namespace Server.Items
{
	[FlipableAttribute( 0x1BD7, 0x1BDA )]
	public class Board : Item, ICommodity
	{
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set { m_Resource = value; InvalidateProperties(); }
		}

		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} {1} board" : "{0} {1} boards", Amount, CraftResources.IsStandard( m_Resource ) ? String.Empty : CraftResources.GetName( m_Resource ).ToLower() );
			}
		}

		[Constructable]
		public Board()
			: this( 1 )
		{
		}

		[Constructable]
		public Board( int amount )
			: this( CraftResource.RegularWood, amount )
		{
		}

		public Board( Serial serial )
			: base( serial )
		{
		}

		[Constructable]
		public Board( CraftResource resource ) : this( resource, 1 )
		{
		}

		[Constructable]
		public Board( CraftResource resource, int amount )
			: base( 0x1BD7 )
		{
			Stackable = true;
			Weight = 2.0;
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

			writer.Write( (int) 2 );

			writer.Write( (int)m_Resource );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
					{
						m_Resource = (CraftResource)reader.ReadInt();
						break;
					}
			}

			if ( version == 0 && Weight == 0.1 )
				Weight = -1;

			if ( version <= 1 )
				m_Resource = CraftResource.RegularWood;
		}
	}


	public class HeartwoodBoard : Board
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

	public class BloodwoodBoard : Board
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

	public class FrostwoodBoard : Board
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

	public class OakBoard : Board
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

	public class AshBoard : Board
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

	public class YewBoard : Board
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