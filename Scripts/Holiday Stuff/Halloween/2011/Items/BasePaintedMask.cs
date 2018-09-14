using System;
//using System.Collections.Generic;

namespace Server.Items.Holiday
{
	[TypeAlias( "Server.Items.ClownMask", "Server.Items.DaemonMask", "Server.Items.PlagueMask" )]
	public class BasePaintedMask : Item
	{
		public override string DefaultName
		{
			get
			{
				if ( m_Staffer != null )
				{
					return $"{MaskName} hand painted by {m_Staffer}";
				}

				return MaskName;
			}
		}

		public virtual string MaskName => "A Mask";

		private string m_Staffer;

		private static string[] m_Staffers =
		{
				  "Ryan",
				  "Mark",
				  "Krrios",
				  "Zippy",
				  "Athena",
				  "Eos",
				  "Xavier"
		};

		public BasePaintedMask( int itemid )
			: this( m_Staffers[ Utility.Random( m_Staffers.Length ) ], itemid )
		{

		}

		public BasePaintedMask( string staffer, int itemid )
			: base( itemid + Utility.Random( 2 ) )
		{
			m_Staffer = staffer;

			Utility.Intern( m_Staffer );
		}

		public BasePaintedMask( Serial serial ) : base( serial )
		{

		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )1 ); // version
			writer.Write( ( string )m_Staffer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version == 1 )
			{
				m_Staffer = Utility.Intern( reader.ReadString() );
			}
		}
	}
}
