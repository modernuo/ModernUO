using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
	public class UnholyBone : Item, ICarvable
	{
		private SpawnTimer m_Timer;

		public override string DefaultName
		{
			get { return "unholy bone"; }
		}

		[Constructable]
		public UnholyBone() : base( 0xF7E )
		{
			Movable = false;
			Hue = 0x497;

			m_Timer = new SpawnTimer( this );
			m_Timer.Start();
		}

		public void Carve( Mobile from, Item item )
		{
			Effects.PlaySound( GetWorldLocation(), Map, 0x48F );
			Effects.SendLocationEffect( GetWorldLocation(), Map, 0x3728, 10, 10, 0, 0 );

			if ( 0.3 > Utility.RandomDouble() )
			{
				if ( ItemID == 0xF7E )
					from.SendMessage( "You destroy the bone." );
				else
					from.SendMessage( "You destroy the bone pile." );

				Gold gold = new Gold( 25, 100 );

				gold.MoveToWorld( GetWorldLocation(), Map );

				Delete();

				m_Timer.Stop();
			}
			else
			{
				if ( ItemID == 0xF7E )
					from.SendMessage( "You damage the bone." );
				else
					from.SendMessage( "You damage the bone pile." );
			}
		}

		public UnholyBone( Serial serial ) : base( serial )
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

			m_Timer = new SpawnTimer( this );
			m_Timer.Start();
		}

		private class SpawnTimer : Timer
		{
			private Item m_Item;

			public SpawnTimer( Item item ) : base( TimeSpan.FromSeconds( Utility.RandomMinMax( 5, 10 ) ) )
			{
				Priority = TimerPriority.FiftyMS;

				m_Item = item;
			}

			protected override void OnTick()
			{
				if ( m_Item.Deleted )
					return;

				Mobile spawn;

				switch ( Utility.Random( 12 ) )
				{
					default:
					case 0: spawn = new Skeleton(); break;
					case 1: spawn = new Zombie(); break;
					case 2: spawn = new Wraith(); break;
					case 3: spawn = new Spectre(); break;
					case 4: spawn = new Ghoul(); break;
					case 5: spawn = new Mummy(); break;
					case 6: spawn = new Bogle(); break;
					case 7: spawn = new RottingCorpse(); break;
					case 8: spawn = new BoneKnight(); break;
					case 9: spawn = new SkeletalKnight(); break;
					case 10: spawn = new Lich(); break;
					case 11: spawn = new LichLord(); break;
				}

				spawn.MoveToWorld( m_Item.Location, m_Item.Map );

				m_Item.Delete();
			}
		}
	}
}