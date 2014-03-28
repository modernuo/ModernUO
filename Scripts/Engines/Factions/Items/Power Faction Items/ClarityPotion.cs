using System;
using System.Collections.Generic;
using System.Text;

using Server;
using Server.Gumps;
using Server.Multis;
using Server.Mobiles;
using Server.Factions;

namespace Server {
	public sealed class ClarityPotion : PowerFactionItem {
		public override string DefaultName {
			get {
				return "clarity potion";
			}
		}

		public ClarityPotion()
			: base( 3628 ) {
			Hue = 1154;
		}

		public ClarityPotion( Serial serial )
			: base( serial ) {
		}

		public override bool Use( Mobile from ) {
			if ( from.BeginAction( typeof( ClarityPotion ) ) ) {
				int amount = Utility.Dice( 3, 3, 3 );
				int time = Utility.RandomMinMax( 5, 30 );

				from.PlaySound( 0x2D6 );

				if ( from.Body.IsHuman ) {
					from.Animate( 34, 5, 1, true, false, 0 );
				}

				from.FixedParticles( 0x375A, 10, 15, 5011, EffectLayer.Head );
				from.PlaySound( 0x1EB );

				StatMod mod = from.GetStatMod( "Concussion" );

				if ( mod != null ) {
					from.RemoveStatMod( "Concussion" );
					from.Mana -= mod.Offset;
				}

				from.PlaySound( 0x1EE );
				from.AddStatMod( new StatMod( StatType.Int, "clarity-potion", amount, TimeSpan.FromMinutes( time ) ) );

				Timer.DelayCall( TimeSpan.FromMinutes( time ), delegate() {
					from.EndAction( typeof( ClarityPotion ) );
				} );

				return true;
			}

			return false;
		}

		public override void Serialize( GenericWriter writer ) {
			base.Serialize( writer );

			writer.WriteEncodedInt( ( int ) 0 ); // version
		}

		public override void Deserialize( GenericReader reader ) {
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}