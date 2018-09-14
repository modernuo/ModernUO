using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
	public class QuestDaemonBlood : QuestItem
	{
		[Constructible]
		public QuestDaemonBlood() : base( 0xF7D )
		{
			Weight = 1.0;
		}

		public QuestDaemonBlood( Serial serial ) : base( serial )
		{
		}

		public override bool CanDrop( PlayerMobile player )
		{
			return !(player.Quest is UzeraanTurmoilQuest);

			/*return !qs.IsObjectiveInProgress( typeof( ReturnDaemonBloodObjective ) );*/
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
