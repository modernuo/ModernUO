using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Taellia : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Taellia() : base( "the wise" )
		{
			Name = "Elder Taellia";

			m_Spoken = DateTime.Now;
		}

		public Taellia( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = true;
			Race = Race.Elf;

			Hue = 0x8385;
			HairItemID = 0x2FCD;
			HairHue = 0x368;
		}

		public override void InitOutfit()
		{
			AddItem( new Boots( 0x74B ) );
			AddItem( new FemaleElvenRobe( 0x44 ) );
			AddItem( new Circlet() );
			AddItem( new Item( 0xDF2 ) );
		}

		private DateTime m_Spoken;

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m.Alive && m is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)m;

				int range = 5;

				if ( range >= 0 && InRange( m, range ) && !InRange( oldLocation, range ) && DateTime.Now >= m_Spoken + TimeSpan.FromMinutes( 1 ) )
				{
					/* Welcome Seeker.  Do you wish to embrace your elven heritage, casting 
					aside your humanity, and accepting the responsibilities of a caretaker 
					of our beloved Sosaria.  Then seek out Darius the Wise in Moonglow.  
					He will place you on the path. */

					Say( 1072800 );

					m_Spoken = DateTime.Now;
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Spoken = DateTime.Now;
		}
	}
}
