using System;
using Server.Mobiles;

namespace Server.Items
{
	[FlipableAttribute( 0x13E3, 0x13E4 )]
	public class HammerOfHephaestus : SmithHammer
	{
		public override int LabelNumber{ get{ return 1077740; } } // Hammer of Hephaestus

		public static readonly TimeSpan RechargeDelay = TimeSpan.FromMinutes( 5 );

		[Constructable]
		public HammerOfHephaestus()
		{
			UsesRemaining = 20;
			LootType = LootType.Blessed;

			// TODO: Blacksmith +10 bonus when equipped

			StartRechargeTimer();
		}

		public override bool BreakOnDepletion { get { return false; } }
		/* Note:
		 * On EA, it also leaves the crafting gump open when it reaches 0 charges.
		 * When crafting again, only then the crafting gump closes with the 1072306 system message.
		 */

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) && Parent != from ) // TODO: These checks don't match EA, but they match BaseTool for now
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			else if ( UsesRemaining <= 0 )
				from.SendLocalizedMessage( 1072306 ); // You must wait a moment for it to recharge.
			else
				base.OnDoubleClick( from );
		}

		private void StartRechargeTimer()
		{
			// TODO: Needs work
			//Timer.DelayCall( RechargeDelay, RechargeDelay, new TimerCallback( Recharge ) );
		}

		public void Recharge()
		{
			// TODO: Stop timer at 20? Count downtime? Something more generic so we can use it for JacobsPickaxe too (both are IUsesRemaining)?
			if ( UsesRemaining < 20 )
				++UsesRemaining;
		}

		public HammerOfHephaestus( Serial serial )
			: base( serial )
		{
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

			StartRechargeTimer();
		}
	}
}
