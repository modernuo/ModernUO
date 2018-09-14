using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Doom
{
	public class BellOfTheDead : Item
	{
		public override int LabelNumber => 1050018; // bell of the dead

		[Constructible]
		public BellOfTheDead() : base( 0x91A )
		{
			Hue = 0x835;
			Movable = false;
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public Chyloth Chyloth { get; set; }

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public SkeletalDragon Dragon { get; set; }

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public bool Summoning { get; set; }

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
				BeginSummon( from );
			else
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
		}

		public virtual void BeginSummon( Mobile from )
		{
			if ( Chyloth != null && !Chyloth.Deleted )
			{
				from.SendLocalizedMessage( 1050010 ); // The ferry man has already been summoned.  There is no need to ring for him again.
			}
			else if ( Dragon != null && !Dragon.Deleted )
			{
				from.SendLocalizedMessage( 1050017 ); // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
			}
			else if ( !Summoning )
			{
				Summoning = true;

				Effects.PlaySound( GetWorldLocation(), Map, 0x100 );

				Timer.DelayCall( TimeSpan.FromSeconds( 8.0 ), new TimerStateCallback( EndSummon ), from );
			}
		}

		public virtual void EndSummon( object state )
		{
			Mobile from = (Mobile)state;

			if ( Chyloth != null && !Chyloth.Deleted )
			{
				from.SendLocalizedMessage( 1050010 ); // The ferry man has already been summoned.  There is no need to ring for him again.
			}
			else if ( Dragon != null && !Dragon.Deleted )
			{
				from.SendLocalizedMessage( 1050017 ); // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
			}
			else if ( Summoning )
			{
				Summoning = false;

				Point3D loc = GetWorldLocation();

				loc.Z -= 16;

				Effects.SendLocationParticles( EffectItem.Create( loc, Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 0, 0, 2023, 0 );
				Effects.PlaySound( loc, Map, 0x1FE );

				Chyloth = new Chyloth();

				Chyloth.Direction = (Direction)(7 & (4 + (int)from.GetDirectionTo( loc )));
				Chyloth.MoveToWorld( loc, Map );

				Chyloth.Bell = this;
				Chyloth.AngryAt = from;
				Chyloth.BeginGiveWarning();
				Chyloth.BeginRemove( TimeSpan.FromSeconds( 40.0 ) );
			}
		}

		public BellOfTheDead( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) Chyloth );
			writer.Write( (Mobile) Dragon );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			Chyloth = reader.ReadMobile() as Chyloth;
			Dragon = reader.ReadMobile() as SkeletalDragon;

			Chyloth?.Delete();
		}
	}
}
