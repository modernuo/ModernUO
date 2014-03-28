using System;
using System.Collections.Generic;
using System.Text;

using Server;
using Server.Gumps;
using Server.Multis;
using Server.Mobiles;
using Server.Factions;

namespace Server {
	public sealed class GemOfEmpowerment : PowerFactionItem {
		public override string DefaultName {
			get {
				return "gem of empowerment";
			}
		}

		public GemOfEmpowerment()
			: base( 7955 ) {
			Hue = 1154;
		}

		public GemOfEmpowerment( Serial serial )
			: base( serial ) {
		}

		public override bool Use( Mobile from ) {
			if ( Faction.ClearSkillLoss( from ) ) {
				from.LocalOverheadMessage( Server.Network.MessageType.Regular, 2219, false, "The gem shatters as you invoke its power." );
				from.PlaySound( 909 );

				from.FixedEffect( 0x373A, 10, 30 );
				from.PlaySound( 0x209 );

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