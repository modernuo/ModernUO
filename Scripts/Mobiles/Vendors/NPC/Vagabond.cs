using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Vagabond : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		[Constructable]
		public Vagabond() : base( "the vagabond" )
		{
			SetSkill( SkillName.ItemID, 60.0, 83.0 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBTinker() );
			m_SBInfos.Add( new SBVagabond() );
		}

		public override void InitOutfit()
		{
			AddItem( new FancyShirt( RandomBrightHue() ) );
			AddItem( new Shoes( GetShoeHue() ) );
			AddItem( new LongPants( GetRandomHue() ) );

			if ( Utility.RandomBool() )
				AddItem( new Cloak( RandomBrightHue() ) );

			switch ( Utility.Random( 2 ) )
			{
				case 0: AddItem( new SkullCap( Utility.RandomNeutralHue() ) ); break;
				case 1: AddItem( new Bandana( Utility.RandomNeutralHue() ) ); break;
			}


			Utility.AssignRandomHair( this );
			Utility.AssignRandomFacialHair( this, HairHue );

			PackGold( 100, 200 );
		}

		public Vagabond( Serial serial ) : base( serial )
		{
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
		}
	}
}