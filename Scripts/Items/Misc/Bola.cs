using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
	public class Bola : Item
	{
		[Constructable]
		public Bola() : this( 1 )
		{
		}

		[Constructable]
		public Bola( int amount ) : base( 0x26AC )
		{
			Weight = 4.0;
			Stackable = true;
			Amount = amount;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1040019 ); // The bola must be in your pack to use it.
			}
			else if ( !from.CanBeginAction( typeof( Bola ) ) )
			{
				from.SendLocalizedMessage( 1049624 ); // You have to wait a few moments before you can use another bola!
			}
			else if ( from.Target is BolaTarget )
			{
				from.SendLocalizedMessage( 1049631 ); // This bola is already being used.
			}
			else if ( !HasFreeHands( from ) )
			{
				from.SendLocalizedMessage( 1040015 ); // Your hands must be free to use this
			}
			else if ( from.Mounted )
			{
				from.SendLocalizedMessage( 1040016 ); // You cannot use this while riding a mount
			}
			else if ( Server.Spells.Ninjitsu.AnimalForm.UnderTransformation( from ) )
			{
				from.SendLocalizedMessage( 1070902 ); // You can't use this while in an animal form!
			}
			else
			{
				EtherealMount.StopMounting( from );

				from.Target = new BolaTarget( this );
				from.LocalOverheadMessage( MessageType.Emote, 0x3B2, 1049632 ); // * You begin to swing the bola...*
				from.NonlocalOverheadMessage( MessageType.Emote, 0x3B2, 1049633, from.Name ); // ~1_NAME~ begins to menacingly swing a bola...
			}
		}

		private static void ReleaseBolaLock( object state )
		{
			((Mobile)state).EndAction( typeof( Bola ) );
		}

		private static void FinishThrow( object state )
		{
			object[] states = (object[])state;

			Mobile from = (Mobile)states[0];
			Mobile to = (Mobile)states[1];

			if ( Core.AOS )
				new Bola().MoveToWorld( to.Location, to.Map );

			if ( to is ChaosDragoon || to is ChaosDragoonElite )
				from.SendLocalizedMessage( 1042047 ); // You fail to knock the rider from its mount.

			IMount mt = to.Mount;
			if ( mt != null && !( to is ChaosDragoon || to is ChaosDragoonElite ) )
				mt.Rider = null;

			if (to is PlayerMobile)
			{
				if (Server.Spells.Ninjitsu.AnimalForm.UnderTransformation(to))
				{
					to.SendLocalizedMessage(1114066, from.Name); // ~1_NAME~ knocked you out of animal form!
				}
				else if (to.Mounted)
				{
					to.SendLocalizedMessage(1040023); // You have been knocked off of your mount!
				}

				(to as PlayerMobile).SetMountBlock(BlockMountType.Dazed, TimeSpan.FromSeconds( Core.ML ? 10 : 3 ), true);
			}

			if (Core.AOS && from is PlayerMobile) /* only failsafe, attacker should already be dismounted */
			{
				(from as PlayerMobile).SetMountBlock( BlockMountType.BolaRecovery, TimeSpan.FromSeconds( Core.ML ? 10 : 3 ), true );
			}

			to.Damage(1);

			Timer.DelayCall( TimeSpan.FromSeconds( 2.0 ), new TimerStateCallback( ReleaseBolaLock ), from );
		}

		private static bool HasFreeHands( Mobile from )
		{
			Item one = from.FindItemOnLayer( Layer.OneHanded );
			Item two = from.FindItemOnLayer( Layer.TwoHanded );

			if ( Core.SE )
			{
				Container pack = from.Backpack;

				if ( pack != null )
				{
					if ( one != null && one.Movable )
					{
						pack.DropItem( one );
						one = null;
					}

					if ( two != null && two.Movable )
					{
						pack.DropItem( two );
						two = null;
					}
				}
			}
			else if ( Core.AOS )
			{
				if ( one != null && one.Movable )
				{
					from.AddToBackpack( one );
					one = null;
				}

				if ( two != null && two.Movable )
				{
					from.AddToBackpack( two );
					two = null;
				}
			}

			return ( one == null && two == null );
		}

		public class BolaTarget : Target
		{
			private Bola m_Bola;

			public BolaTarget( Bola bola ) : base( 8, false, TargetFlags.Harmful )
			{
				m_Bola = bola;
			}

			protected override void OnTarget( Mobile from, object obj )
			{
				if ( m_Bola.Deleted )
					return;

				if ( obj is Mobile )
				{
					Mobile to = (Mobile)obj;

					if ( !m_Bola.IsChildOf( from.Backpack ) )
					{
						from.SendLocalizedMessage( 1040019 ); // The bola must be in your pack to use it.
					}
					else if ( !HasFreeHands( from ) )
					{
						from.SendLocalizedMessage( 1040015 ); // Your hands must be free to use this
					}
					else if ( from.Mounted )
					{
						from.SendLocalizedMessage( 1040016 ); // You cannot use this while riding a mount
					}
					else if ( Server.Spells.Ninjitsu.AnimalForm.UnderTransformation( from ) )
					{
						from.SendLocalizedMessage( 1070902 ); // You can't use this while in an animal form!
					}
					else if (!to.Mounted && !Server.Spells.Ninjitsu.AnimalForm.UnderTransformation(to))
					{
						from.SendLocalizedMessage( 1049628 ); // You have no reason to throw a bola at that.
					}
					else if ( !from.CanBeHarmful( to ) )
					{
					}
					else if ( from.BeginAction( typeof( Bola ) ) )
					{
						EtherealMount.StopMounting( from );

						from.DoHarmful( to );

						m_Bola.Consume();

						from.Direction = from.GetDirectionTo( to );
						from.Animate( 11, 5, 1, true, false, 0 );
						from.MovingEffect( to, 0x26AC, 10, 0, false, false );

						Timer.DelayCall( TimeSpan.FromSeconds( 0.5 ), new TimerStateCallback( FinishThrow ), new object[]{ from, to } );
					}
					else
					{
						from.SendLocalizedMessage( 1049624 ); // You have to wait a few moments before you can use another bola!
					}
				}
				else
				{
					from.SendLocalizedMessage( 1049629 ); // You cannot throw a bola at that.
				}
			}
		}

		public Bola( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}