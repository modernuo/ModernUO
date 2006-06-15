using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class TinkerGuildmaster : BaseGuildmaster
	{
		public override NpcGuild NpcGuild{ get{ return NpcGuild.TinkersGuild; } }

		[Constructable]
		public TinkerGuildmaster() : base( "tinker" )
		{
			SetSkill( SkillName.Lockpicking, 65.0, 88.0 );
			SetSkill( SkillName.Tinkering, 90.0, 100.0 );
			SetSkill( SkillName.RemoveTrap, 85.0, 100.0 );
		}

		public TinkerGuildmaster( Serial serial ) : base( serial )
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