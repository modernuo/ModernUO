using System;
using Server;
using Server.Items;

namespace Server.Factions
{
	public class FactionTrapRemovalKit : Item
	{
		private int m_Charges;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Charges
		{
			get{ return m_Charges; }
			set{ m_Charges = value; }
		}

		public override int LabelNumber{ get{ return 1041508; } } // a faction trap removal kit

		[Constructable]
		public FactionTrapRemovalKit() : base( 7867 )
		{
			LootType = LootType.Blessed;
			m_Charges = 25;
		}

		public void ConsumeCharge( Mobile consumer )
		{
			--m_Charges;

			if ( m_Charges <= 0 )
			{
				Delete();

				if ( consumer != null )
					consumer.SendLocalizedMessage( 1042531 ); // You have used all of the parts in your trap removal kit.
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			// NOTE: OSI does not list uses remaining; intentional difference
			list.Add( 1060584, m_Charges.ToString() ); // uses remaining: ~1_val~
		}

		public FactionTrapRemovalKit( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.WriteEncodedInt( (int) m_Charges );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_Charges = reader.ReadEncodedInt();
					break;
				}
				case 0:
				{
					m_Charges = 25;
					break;
				}
			}
		}
	}
}