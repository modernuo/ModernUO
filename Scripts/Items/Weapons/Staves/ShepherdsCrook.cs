using System;
using Server.Network;
using Server.Items;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Items
{
	[FlipableAttribute( 0xE81, 0xE82 )]
	public class ShepherdsCrook : BaseStaff
	{
		public override WeaponAbility PrimaryAbility{ get{ return WeaponAbility.CrushingBlow; } }
		public override WeaponAbility SecondaryAbility{ get{ return WeaponAbility.Disarm; } }

		public override int AosStrengthReq{ get{ return 20; } }
		public override int AosMinDamage{ get{ return 13; } }
		public override int AosMaxDamage{ get{ return 15; } }
		public override int AosSpeed{ get{ return 40; } }

		public override int OldStrengthReq{ get{ return 10; } }
		public override int OldMinDamage{ get{ return 3; } }
		public override int OldMaxDamage{ get{ return 12; } }
		public override int OldSpeed{ get{ return 30; } }

		public override int InitMinHits{ get{ return 31; } }
		public override int InitMaxHits{ get{ return 50; } }

		[Constructable]
		public ShepherdsCrook() : base( 0xE81 )
		{
			Weight = 4.0;
		}

		public ShepherdsCrook( Serial serial ) : base( serial )
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

			if ( Weight == 2.0 )
				Weight = 4.0;
		}

		public override void OnDoubleClick( Mobile from )
		{
			from.SendLocalizedMessage( 502464 ); // Target the animal you wish to herd.
			from.Target = new HerdingTarget();
		}

		private class HerdingTarget : Target
		{
			public HerdingTarget() : base( 10, false, TargetFlags.None )
			{
			}

			protected override void OnTarget( Mobile from, object targ )
			{
				if ( targ is BaseCreature )
				{
					BaseCreature bc = (BaseCreature)targ;

					if ( bc.Body.IsAnimal )
					{
						if ( bc.Controlled )
						{
							bc.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502467, from.NetState ); // That animal looks tame already.
						}
						else 
						{
							from.SendLocalizedMessage( 502475 ); // Click where you wish the animal to go.
							from.Target = new InternalTarget( bc );
						}
					}
					else
					{
						from.SendLocalizedMessage( 502468 ); // That is not a herdable animal.
					}
				}
				else
				{
					from.SendLocalizedMessage( 502472 ); // You don't seem to be able to persuade that to move.
				}
			}

			private class InternalTarget : Target
			{
				private BaseCreature m_Creature;

				public InternalTarget( BaseCreature c ) : base( 10, true, TargetFlags.None )
				{
					m_Creature = c;
				}

				protected override void OnTarget( Mobile from, object targ )
				{
					if ( targ is IPoint2D )
					{
						if ( from.CheckTargetSkill( SkillName.Herding, m_Creature, 0, 100 ) )
						{
							m_Creature.TargetLocation = new Point2D( (IPoint2D)targ );
							from.SendLocalizedMessage( 502479 ); // The animal walks where it was instructed to.
						}
						else
						{
							from.SendLocalizedMessage( 502472 ); // You don't seem to be able to persuade that to move.
						}
					}
				}
			}
		}
	}
}
