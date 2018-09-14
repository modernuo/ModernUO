using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
	public class QuestDaemonBone : QuestItem
	{
		[Constructible]
		public QuestDaemonBone() : base( 0xF80 )
		{
			Weight = 1.0;
		}

		public QuestDaemonBone( Serial serial ) : base( serial )
		{
		}

		public override bool CanDrop( PlayerMobile player )
		{
			return !(player.Quest is UzeraanTurmoilQuest);

			//return !qs.IsObjectiveInProgress( typeof( ReturnDaemonBoneObjective ) );
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
