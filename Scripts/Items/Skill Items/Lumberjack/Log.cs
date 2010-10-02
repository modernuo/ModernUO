using System;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x1bdd, 0x1be0 )]
	public abstract class BaseLog : Item, ICommodity, IAxe
	{
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set { m_Resource = value; InvalidateProperties(); }
		}
		
		#region Old Log Serialization Vars
		/* DO NOT USE! Only used in serialization of logs that originally derived from Log */
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
				return String.Format( Amount == 1 ? "{0} {1} log" : "{0} {1} logs", Amount, CraftResources.IsStandard( m_Resource ) ? String.Empty : CraftResources.GetName( m_Resource ).ToLower() );
			}
		}

		int ICommodity.DescriptionNumber { get { return CraftResources.IsStandard( m_Resource ) ? LabelNumber : 1075062 + ( (int)m_Resource - (int)CraftResource.RegularWood ); } }

		public BaseLog( CraftResource resource, int amount )
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
		public BaseLog( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version

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
				/** Below is for deserialization of old logs that orignally inherited from Log or Item **/
				case 1: // For all logs
				{
					m_Resource = (CraftResource)reader.ReadInt();
					goto case 0;
				}
				case 0: // For old standard logs
				{
					m_InheritsItem = true;
					m_OldVersion = version;
					break;
				}
			}
		}

		public virtual bool TryCreateBoards( Mobile from, double skill, Item item )
		{
			if ( Deleted || !from.CanSee( this ) ) 
				return false;
			else if ( from.Skills.Carpentry.Value < skill &&
				from.Skills.Lumberjacking.Value < skill )
			{
				from.SendLocalizedMessage( 1072652 ); // You cannot work this strange and unusual wood.
				return false;
			}
			base.ScissorHelper( from, item, 1, false );
			return true;
		}

		public virtual bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 0, new Board() ) )
				return false;
			
			return true;
		}
	}

	public class Log : BaseLog
	{
		[Constructable]
		public Log() : this( 1 )
		{
		}

		[Constructable]
		public Log( int amount ) 
			: base( CraftResource.RegularWood, amount )
		{
		}

		public Log( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); // Required for BaseLog insertion

			switch ( version )
			{
				/** Versions 0 and 1 originally inherited from Item. Version 0 is from before Log became a CraftResource. **/
				case 0:
				{
					Resource = CraftResource.RegularWood;
					break;
				}
			}
		}

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 0, new Board() ) )
				return false;
			
			return true;
		}
	}
	
	public class HeartwoodLog : BaseLog
	{
		[Constructable]
		public HeartwoodLog() : this( 1 )
		{
		}
		[Constructable]
		public HeartwoodLog( int amount ) 
			: base( CraftResource.Heartwood, amount )
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 100, new HeartwoodBoard() ) )
				return false;

			return true;
		}
	}

	public class BloodwoodLog : BaseLog
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 100, new BloodwoodBoard() ) )
				return false;

			return true;
		}
	}

	public class FrostwoodLog : BaseLog
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 100, new FrostwoodBoard() ) )
				return false;

			return true;
		}
	}

	public class OakLog : BaseLog
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 65, new OakBoard() ) )
				return false;

			return true;
		}
	}

	public class AshLog : BaseLog
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 80, new AshBoard() ) )
				return false;

			return true;
		}
	}

	public class YewLog : BaseLog
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

		public override bool Axe( Mobile from, BaseAxe axe )
		{
			if ( !TryCreateBoards( from , 95, new YewBoard() ) )
				return false;

			return true;
		}
	}
}