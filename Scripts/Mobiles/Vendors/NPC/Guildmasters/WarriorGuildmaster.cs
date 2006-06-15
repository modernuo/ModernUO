using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class WarriorGuildmaster : BaseGuildmaster
	{
		public override NpcGuild NpcGuild{ get{ return NpcGuild.WarriorsGuild; } }

		[Constructable]
		public WarriorGuildmaster() : base( "warrior" )
		{
			SetSkill( SkillName.ArmsLore, 75.0, 98.0 );
			SetSkill( SkillName.Parry, 85.0, 100.0 );
			SetSkill( SkillName.MagicResist, 60.0, 83.0 );
			SetSkill( SkillName.Tactics, 85.0, 100.0 );
			SetSkill( SkillName.Swords, 90.0, 100.0 );
			SetSkill( SkillName.Macing, 60.0, 83.0 );
			SetSkill( SkillName.Fencing, 60.0, 83.0 );
		}

		public WarriorGuildmaster( Serial serial ) : base( serial )
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