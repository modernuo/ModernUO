using System;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests.Ninja
{
	public class EminosKatana : QuestItem
	{
		public override int LabelNumber => 1063214; // Daimyo Emino's Katana

		[Constructible]
		public EminosKatana() : base( 0x13FF )
		{
			Weight = 1.0;
		}

		public EminosKatana( Serial serial ) : base( serial )
		{
		}

		public override bool CanDrop( PlayerMobile player )
		{
			return !(player.Quest is EminosUndertakingQuest);

			/*return !qs.IsObjectiveInProgress( typeof( ReturnSwordObjective ) )
				&& !qs.IsObjectiveInProgress( typeof( SlayHenchmenObjective ) )
				&& !qs.IsObjectiveInProgress( typeof( GiveEminoSwordObjective ) );*/
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
