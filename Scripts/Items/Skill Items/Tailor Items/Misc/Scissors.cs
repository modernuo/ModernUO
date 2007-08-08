using System;
using Server.Network;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Items
{
	public interface IScissorable
	{
		bool Scissor( Mobile from, Scissors scissors );
	}

	[FlipableAttribute( 0xf9f, 0xf9e )]
	public class Scissors : Item
	{
		[Constructable]
		public Scissors() : base( 0xF9F )
		{
			Weight = 1.0;
		}

		public Scissors( Serial serial ) : base( serial )
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

		public override void OnDoubleClick( Mobile from )
		{
			from.SendLocalizedMessage( 502434 ); // What should I use these scissors on?

			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private Scissors m_Item;

			public InternalTarget( Scissors item ) : base( 1, false, TargetFlags.None )
			{
				m_Item = item;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Item.Deleted )
					return;

				/*if ( targeted is Item && !((Item)targeted).IsStandardLoot() )
				{
					from.SendLocalizedMessage( 502440 ); // Scissors can not be used on that to produce anything.
				}
				else */
				if( Core.AOS && targeted == from )
				{
					from.SendLocalizedMessage( 1062845 + Utility.Random( 3 ) );	//"That doesn't seem like the smartest thing to do." / "That was an encounter you don't wish to repeat." / "Ha! You missed!"
				}
				else if( Core.SE && Utility.RandomDouble() > .20 && (from.Direction & Direction.Running) != 0 && ( DateTime.Now - from.LastMoveTime ) < from.ComputeMovementSpeed( from.Direction ) )
				{
					from.SendLocalizedMessage( 1063305 ); // Didn't your parents ever tell you not to run with scissors in your hand?!
				}
				else if ( targeted is Item && !((Item)targeted).Movable ) {
					return;
				}
				else if( targeted is IScissorable )
				{
					IScissorable obj = (IScissorable)targeted;

					if( obj.Scissor( from, m_Item ) )
						from.PlaySound( 0x248 );
				}
				else
				{
					from.SendLocalizedMessage( 502440 ); // Scissors can not be used on that to produce anything.
				}
			}
		}
	}
}