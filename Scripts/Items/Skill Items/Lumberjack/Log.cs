using System;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x1bdd, 0x1be0 )]
	public class Log : Item, ICommodity
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
				return String.Format( Amount == 1 ? "{0} {1} log" : "{0} {1} logs", Amount, CraftResources.IsStandard( m_Resource ) ? String.Empty : CraftResources.GetName( m_Resource ).ToLower() );
			}
		}

		[Constructable]
		public Log() : this( 1 )
		{
		}

		[Constructable]
		public Log( int amount ) : this( CraftResource.RegularWood )
		{
		}

		[Constructable]
		public Log( CraftResource resource )
			: this( resource, 1 )
		{
		}

		[Constructable]
		public Log( CraftResource resource, int amount )
			: base( 0x1BDD )
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

		public Log( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( (int)m_Resource );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
					{
						m_Resource = (CraftResource)reader.ReadInt();
						break;
					}
			}

			if ( version == 0 )
				m_Resource = CraftResource.RegularWood;
		}
	}

	public class HeartwoodLog : Log
	{
		[Constructable]
		public HeartwoodLog() : this( 1 )
		{
		}

		[Constructable]
		public HeartwoodLog( int amount ) : base( CraftResource.Heartwood, amount )
		{
		}
		
		public HeartwoodLog( Serial serial ) : base( serial )
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

	public class BloodwoodLog : Log
	{
		[Constructable]
		public BloodwoodLog()
			: this( 1 )
		{
		}

		[Constructable]
		public BloodwoodLog( int amount )
			: base( CraftResource.Bloodwood, amount )
		{
		}

		public BloodwoodLog( Serial serial )
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

	public class FrostwoodLog : Log
	{
		[Constructable]
		public FrostwoodLog()
			: this( 1 )
		{
		}

		[Constructable]
		public FrostwoodLog( int amount )
			: base( CraftResource.Frostwood, amount )
		{
		}

		public FrostwoodLog( Serial serial )
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

	public class OakLog : Log
	{
		[Constructable]
		public OakLog()
			: this( 1 )
		{
		}

		[Constructable]
		public OakLog( int amount )
			: base( CraftResource.OakWood, amount )
		{
		}

		public OakLog( Serial serial )
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

	public class AshLog : Log
	{
		[Constructable]
		public AshLog()
			: this( 1 )
		{
		}

		[Constructable]
		public AshLog( int amount )
			: base( CraftResource.AshWood, amount )
		{
		}

		public AshLog( Serial serial )
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

	public class YewLog : Log
	{
		[Constructable]
		public YewLog()
			: this( 1 )
		{
		}

		[Constructable]
		public YewLog( int amount )
			: base( CraftResource.YewWood, amount )
		{
		}

		public YewLog( Serial serial )
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