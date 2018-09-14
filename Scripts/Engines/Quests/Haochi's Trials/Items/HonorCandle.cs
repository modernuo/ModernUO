using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
	public class HonorCandle : CandleLong
	{
		private static readonly TimeSpan LitDuration = TimeSpan.FromSeconds( 20.0 );

		public override int LitSound => 0;
		public override int UnlitSound => 0;

		[Constructible]
		public HonorCandle()
		{
			Movable = false;
			Duration = LitDuration;
		}

		public HonorCandle( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			bool wasBurning = Burning;

			base.OnDoubleClick( from );

			if ( !wasBurning && Burning )
			{
				if ( !(from is PlayerMobile player) )
					return;

				QuestSystem qs = player.Quest;

				if ( qs is HaochisTrialsQuest )
				{
					QuestObjective obj = qs.FindObjective( typeof( SixthTrialIntroObjective ) );

					if ( obj != null && !obj.Completed )
						obj.Complete();

					SendLocalizedMessageTo( from, 1063251 ); // You light a candle in honor.
				}
			}
		}

		public override void Burn()
		{
			Douse();
		}

		public override void Douse()
		{
			base.Douse();

			Duration = LitDuration;
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
