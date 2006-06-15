using System;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests.Ninja
{
	public class Henchman : BaseCreature
	{
		[Constructable]
		public Henchman() : base( AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			InitStats( 45, 30, 5 );

			Hue = Utility.RandomSkinHue();
			Body = 0x190;
			Name = "a henchman";

			Utility.AssignRandomHair( this );
			Utility.AssignRandomFacialHair( this );

			AddItem( new LeatherNinjaJacket() );
			AddItem( new LeatherNinjaPants() );
			AddItem( new NinjaTabi() );

			if ( Utility.RandomBool() )
				AddItem( new Kama() );
			else
				AddItem( new Tessen() );

			SetSkill( SkillName.Swords, 50.0 );
			SetSkill( SkillName.Tactics, 50.0 );
		}

		public override bool AlwaysMurderer{ get{ return true; } }

		public Henchman( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}