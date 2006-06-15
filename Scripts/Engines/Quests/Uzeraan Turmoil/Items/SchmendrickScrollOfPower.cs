using System;
using Server;
using Server.Mobiles;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Haven
{
	public class SchmendrickScrollOfPower : QuestItem
	{
		public override int LabelNumber { get { return 1049118; } } // a scroll with ancient markings

		public SchmendrickScrollOfPower() : base( 0xE34 )
		{
			Weight = 1.0;
			Hue = 0x34D;
		}

		public SchmendrickScrollOfPower( Serial serial ) : base( serial )
		{
		}

		public override bool CanDrop( PlayerMobile player )
		{
			UzeraanTurmoilQuest qs = player.Quest as UzeraanTurmoilQuest;

			if ( qs == null )
				return true;

			return !qs.IsObjectiveInProgress( typeof( ReturnScrollOfPowerObjective ) );
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