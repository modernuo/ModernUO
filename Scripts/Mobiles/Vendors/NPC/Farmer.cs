using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class Farmer : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		[Constructable]
		public Farmer() : base( "the farmer" )
		{
			SetSkill( SkillName.Lumberjacking, 36.0, 68.0 );
			SetSkill( SkillName.TasteID, 36.0, 68.0 );
			SetSkill( SkillName.Cooking, 36.0, 68.0 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBFarmer() );
		}

		public override VendorShoeType ShoeType
		{
			get{ return VendorShoeType.ThighBoots; }
		}

		public override int GetShoeHue()
		{
			return 0;
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new Server.Items.WideBrimHat( Utility.RandomNeutralHue() ) );
		}

		public Farmer( Serial serial ) : base( serial )
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