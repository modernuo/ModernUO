using System;
using Server.Mobiles;
using Server.Targeting;
using Server.Items;
using Server;
using Server.Engines.Craft;

namespace Server.Factions
{
	public abstract class BaseFactionTrapDeed : Item, ICraftable
	{
		public abstract Type TrapType{ get; }

		private Faction m_Faction;

		[CommandProperty( AccessLevel.GameMaster )]
		public Faction Faction
		{
			get{ return m_Faction; }
			set
			{
				m_Faction = value;

				if ( m_Faction != null )
					Hue = m_Faction.Definition.HuePrimary;
			}
		}

		public BaseFactionTrapDeed( int itemID ) : base( itemID )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
		}

		public BaseFactionTrapDeed( bool createdFromDeed ) : this( 0x14F0 )
		{
		}

		public BaseFactionTrapDeed( Serial serial ) : base( serial )
		{
		}

		public virtual BaseFactionTrap Construct( Mobile from )
		{
			try{ return Activator.CreateInstance( TrapType, new object[]{ m_Faction, from } ) as BaseFactionTrap; }
			catch{ return null; }
		}

		public override void OnDoubleClick( Mobile from )
		{
			Faction faction = Faction.Find( from );

			if ( faction == null )
				from.SendLocalizedMessage( 1010353, "", 0x23 ); // Only faction members may place faction traps
			else if ( faction != m_Faction )
				from.SendLocalizedMessage( 1010354, "", 0x23 ); // You may only place faction traps created by your faction
			else if( faction.Traps.Count >= faction.MaximumTraps )
				from.SendLocalizedMessage( 1010358, "", 0x23 ); // Your faction already has the maximum number of traps placed
			else 
			{
				BaseFactionTrap trap = Construct( from );

				if ( trap == null )
					return;

				int message = trap.IsValidLocation( from.Location, from.Map );

				if ( message > 0 )
				{
					from.SendLocalizedMessage( message, "", 0x23 );
					trap.Delete();
				}
				else
				{
					from.SendLocalizedMessage( 1010360 ); // You arm the trap and carefully hide it from view
					trap.MoveToWorld( from.Location, from.Map );
					faction.Traps.Add( trap );
					Delete();
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Faction.WriteReference( writer, m_Faction );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Faction = Faction.ReadReference( reader );
		}
		#region ICraftable Members

		public int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			ItemID = 0x14F0;
			Faction = Faction.Find( from );

			return 1;
		}

		#endregion
	}
}