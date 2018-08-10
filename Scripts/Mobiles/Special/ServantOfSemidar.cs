using System;
using Server;

namespace Server.Mobiles
{
	public class ServantOfSemidar : BaseCreature
	{
		public override string DefaultName => "a Servant of Semidar";

		[Constructible]
		public ServantOfSemidar() : base( AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4 )
		{
			Body = 0x26;
		}

		public override bool DisallowAllMoves => true;

		public override bool InitialInnocent => true;

		public override bool CanBeDamaged()
		{
			return false;
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			list.Add( 1005494 ); // enslaved
		}

		public ServantOfSemidar( Serial serial ) : base( serial )
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
		}
	}
}
