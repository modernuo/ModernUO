namespace Server.Mobiles
{
	public class FisherGuildmaster : BaseGuildmaster
	{
		public override NpcGuild NpcGuild => NpcGuild.FishermensGuild;

		[Constructible]
		public FisherGuildmaster() : base( "fisher" )
		{
			SetSkill( SkillName.Fishing, 80.0, 100.0 );
		}

		public FisherGuildmaster( Serial serial ) : base( serial )
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
