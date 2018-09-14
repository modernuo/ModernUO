using Server.Items;

namespace Server.Mobiles
{
	public class HarborMaster : BaseCreature
	{
		public override bool CanTeach => false;

		[Constructible]
		public HarborMaster()
			: base( AIType.AI_Animal, FightMode.None, 10, 1, 0.2, 0.4 )
		{
			InitStats( 31, 41, 51 );

			SetSkill( SkillName.Mining, 36, 68 );


			SpeechHue = Utility.RandomDyedHue();
			Hue = Utility.RandomSkinHue();
			Blessed = true;


			if ( Female = Utility.RandomBool() )
			{
				Body = 0x191;
				Name = NameList.RandomName( "female" );
				Title = "the Harbor Mistress";
			}
			else
			{
				Body = 0x190;
				Name = NameList.RandomName( "male" );
				Title = "the Harbor Master";
			}
			AddItem( new Shirt( Utility.RandomDyedHue() ) );
			AddItem( new Boots() );
			AddItem( new LongPants( Utility.RandomNeutralHue() ) );
			AddItem( new QuarterStaff() );

			Utility.AssignRandomHair( this );

			Container pack = new Backpack();

			pack.DropItem( new Gold( 250, 300 ) );

			pack.Movable = false;

			AddItem( pack );
		}

		public override bool ClickTitle => false;


		public HarborMaster( Serial serial )
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
