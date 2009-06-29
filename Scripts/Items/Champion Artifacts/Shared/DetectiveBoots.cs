using System;
using Server;

namespace Server.Items
{
	public class DetectiveBoots : Boots
	{
		public override int LabelNumber{ get{ return 1094894 + m_Level; } } // [Quality] Detective of the Royal Guard [Replica]

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		public override bool CanFortify{ get{ return false; } }

		private int m_Level;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Level
		{
			get{ return m_Level; }
			set{ m_Level = Math.Max( Math.Min( 2, value), 0 ); Attributes.BonusInt = 2 + m_Level; InvalidateProperties(); }
		}

		[Constructable]
		public DetectiveBoots()
		{
			Level = Utility.RandomMinMax( 0, 2 );
		}

		public DetectiveBoots( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			Level = Attributes.BonusInt - 2;
		}
	}
}
