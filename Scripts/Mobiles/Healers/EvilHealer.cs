using System;
using Server;

namespace Server.Mobiles
{
	public class EvilHealer : BaseHealer
	{
		public override bool CanTeach{ get{ return true; } }

		public override bool CheckTeach( SkillName skill, Mobile from )
		{
			if ( !base.CheckTeach( skill, from ) )
				return false;

			return ( skill == SkillName.Forensics )
				|| ( skill == SkillName.Healing )
				|| ( skill == SkillName.SpiritSpeak )
				|| ( skill == SkillName.Swords );
		}

		[Constructable]
		public EvilHealer()
		{
			Title = "the healer";

			Karma = -10000;

			SetSkill( SkillName.Forensics, 80.0, 100.0 );
			SetSkill( SkillName.SpiritSpeak, 80.0, 100.0 );
			SetSkill( SkillName.Swords, 80.0, 100.0 );
		}

		public override bool AlwaysMurderer{ get{ return true; } }
		public override bool IsActiveVendor{ get{ return true; } }

		public override void InitSBInfo()
		{
			SBInfos.Add( new SBHealer() );
		}

		public override bool CheckResurrect( Mobile m )
		{
			return true;
		}

		public EvilHealer( Serial serial ) : base( serial )
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