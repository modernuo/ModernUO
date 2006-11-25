using System;
using Server;
using Server.Targeting;
using System.Collections;
using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{
	[FlipableAttribute( 0x2790, 0x27DB )]
	public class LeatherNinjaBelt : BaseWaist, IUsesRemaining
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.RegularLeather; } }

		private bool m_Using;
		private int m_UsesRemaining;

		private Poison m_Poison;
		private int m_PoisonCharges;

		[CommandProperty( AccessLevel.GameMaster )]
		public int UsesRemaining
		{
			get { return m_UsesRemaining; }
			set { m_UsesRemaining = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Poison Poison
		{
			get{ return m_Poison; }
			set{ m_Poison = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int PoisonCharges
		{
			get { return m_PoisonCharges; }
			set { m_PoisonCharges = value; InvalidateProperties(); }
		}

		public bool ShowUsesRemaining{ get{ return true; } set{} }

		[Constructable]
		public LeatherNinjaBelt() : base( 0x2790 )
		{
			Weight = 1.0;
			Layer = Layer.Waist;
		}

		public LeatherNinjaBelt( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1060584, m_UsesRemaining.ToString() ); // uses remaining: ~1_val~

			if ( m_Poison != null && m_PoisonCharges > 0 )
				list.Add( 1062412 + m_Poison.Level, m_PoisonCharges.ToString() );
		}

		public override bool OnEquip( Mobile from )
		{
			if ( !base.OnEquip( from ) )
				return false;

			from.SendLocalizedMessage( 1070785 ); // Double click this item each time you wish to throw a shuriken.
			return true;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from ) )
				return;

			if ( m_UsesRemaining < 1 )
			{
				// You have no shuriken in your ninja belt!
				from.SendLocalizedMessage( 1063297 );
			}
			else if ( m_Using )
			{
				// You cannot throw another shuriken yet.
				from.SendLocalizedMessage( 1063298 );
			}
			else if ( !BasePotion.HasFreeHand( from ) )
			{
				// You must have a free hand to throw shuriken.
				from.SendLocalizedMessage( 1063299 );
			}
			else
			{
				from.BeginTarget( 10, false, TargetFlags.Harmful, new TargetCallback( OnTarget ) );
			}
		}

		public void Shoot( Mobile from, Mobile target )
		{
			if ( from == target )
				return;

			if ( m_UsesRemaining < 1 )
			{
				// You have no shuriken in your ninja belt!
				from.SendLocalizedMessage( 1063297 );
			}
			else if ( m_Using )
			{
				// You cannot throw another shuriken yet.
				from.SendLocalizedMessage( 1063298 );
			}
			else if ( !BasePotion.HasFreeHand( from ) )
			{
				// You must have a free hand to throw shuriken.
				from.SendLocalizedMessage( 1063299 );
			}
			else if ( from.InRange( target, 2 ) )
			{
				from.SendLocalizedMessage( 1063303 ); // Your target is too close!
			}
			else if ( from.CanBeHarmful( target ) )
			{
				m_Using = true;

				from.Direction = from.GetDirectionTo( target );

				from.RevealingAction();

				if ( from.Body.IsHuman )
					from.Animate( from.Mounted ? 26 : 9, 7, 1, true, false, 0 );

				from.PlaySound( 0x23A );
				from.MovingEffect( target, 0x27AC, 1, 0, false, false );

				if ( from.CheckSkill( SkillName.Ninjitsu, -10.0, 65.0 ) )
					Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( OnShurikenHit ), new object[]{ from, target } );
				else
					ConsumeUse();

				Timer.DelayCall( TimeSpan.FromSeconds( 2.5 ), new TimerCallback( ResetUsing ) );
			}
		}

		private void OnShurikenHit( object state )
		{
			object[] states = (object[])state;
			Mobile from = (Mobile)states[0];
			Mobile target = (Mobile)states[1];

			if ( !from.CanBeHarmful( target ) )
				return;

			from.DoHarmful( target );

			AOS.Damage( target, from, Utility.RandomMinMax( 3, 5 ), 100, 0, 0, 0, 0 );

			if ( m_Poison != null && m_PoisonCharges > 0 )
				target.ApplyPoison( from, m_Poison );

			ConsumeUse();
		}

		public void ConsumeUse()
		{
			if ( m_UsesRemaining < 1 )
				return;

			--m_UsesRemaining;

			if ( m_PoisonCharges > 0 )
			{
				--m_PoisonCharges;

				if ( m_PoisonCharges == 0 )
					m_Poison = null;
			}

			InvalidateProperties();
		}

		public void ResetUsing()
		{
			m_Using = false;
		}

		private const int MaxUses = 10;

		public void Unload( Mobile from )
		{
			if ( UsesRemaining < 1 )
				return;

			Shuriken shuriken = new Shuriken( UsesRemaining );

			shuriken.Poison = m_Poison;
			shuriken.PoisonCharges = m_PoisonCharges;

			from.AddToBackpack( shuriken );

			m_UsesRemaining = 0;
			m_PoisonCharges = 0;
			m_Poison = null;

			InvalidateProperties();
		}

		public void Reload( Mobile from, Shuriken shuriken )
		{
			int need = ( MaxUses - m_UsesRemaining );

			if ( need <= 0 )
			{
				// You cannot add any more shuriken.
				from.SendLocalizedMessage( 1063302 );
			}
			else if ( shuriken.UsesRemaining > 0 )
			{
				if ( need > shuriken.UsesRemaining )
					need = shuriken.UsesRemaining;

				if ( shuriken.Poison != null && shuriken.PoisonCharges > 0 )
				{
					if ( m_PoisonCharges <= 0 || m_Poison == shuriken.Poison )
					{
						if ( m_Poison != null && m_Poison.Level < shuriken.Poison.Level )
							Unload( from );

						if ( need > shuriken.PoisonCharges )
							need = shuriken.PoisonCharges;

						if ( m_Poison == null || m_PoisonCharges <= 0 )
							m_PoisonCharges = need;
						else
							m_PoisonCharges += need;

						m_Poison = shuriken.Poison;

						shuriken.PoisonCharges -= need;

						if ( shuriken.PoisonCharges <= 0 )
							shuriken.Poison = null;

						m_UsesRemaining += need;
						shuriken.UsesRemaining -= need;
					}
					else
					{
						from.SendLocalizedMessage( 1070767 ); // Loaded projectile is stronger, unload it first
					}
				}
				else
				{
					m_UsesRemaining += need;
					shuriken.UsesRemaining -= need;
				}

				if ( shuriken.UsesRemaining <= 0 )
					shuriken.Delete();

				InvalidateProperties();
			}
		}

		public void OnTarget( Mobile from, object obj )
		{
			if ( Deleted || !IsChildOf( from ) )
				return;

			if ( obj is Mobile )
				Shoot( from, (Mobile) obj );
			else if ( obj is Shuriken )
				Reload( from, (Shuriken) obj );
			else
				from.SendLocalizedMessage( 1063301 ); // You can only place shuriken in a ninja belt.
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( IsChildOf( from ) )
			{
				list.Add( new LoadEntry( this ) );
				list.Add( new UnloadEntry( this ) );
			}
		}

		private class LoadEntry : ContextMenuEntry
		{
			private LeatherNinjaBelt m_Belt;

			public LoadEntry( LeatherNinjaBelt belt ) : base( 6222, 0 )
			{
				m_Belt = belt;
			}

			public override void OnClick()
			{
				if ( !m_Belt.Deleted && m_Belt.IsChildOf( Owner.From ) )
					Owner.From.BeginTarget( 10, false, TargetFlags.Harmful, new TargetCallback( m_Belt.OnTarget ) );
			}
		}

		private class UnloadEntry : ContextMenuEntry
		{
			private LeatherNinjaBelt m_Belt;

			public UnloadEntry( LeatherNinjaBelt belt ) : base( 6223, 0 )
			{
				m_Belt = belt;

				Enabled = ( belt.UsesRemaining > 0 );
			}

			public override void OnClick()
			{
				if ( !m_Belt.Deleted && m_Belt.IsChildOf( Owner.From ) )
					m_Belt.Unload( Owner.From );
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (int) m_UsesRemaining );

			Poison.Serialize( m_Poison, writer );
			writer.Write( (int) m_PoisonCharges );
		}
		
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_UsesRemaining = reader.ReadInt();

					m_Poison = Poison.Deserialize( reader );
					m_PoisonCharges = reader.ReadInt();

					break;
				}
			}
		}
	}
}