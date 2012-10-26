using System;
using System.Reflection;
using Server.Items;
using Server.Targeting;
using Server.Events.Halloween;
using Server.Commands;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Engines.Events
{
	public class TrickOrTreat
	{
		public static void Initialize()
		{
			DateTime now = DateTime.Now;

			if( DateTime.Now >= HolidaySettings.StartHalloween && DateTime.Now <= HolidaySettings.FinishHalloween )
			{
				EventSink.Speech += new SpeechEventHandler( EventSink_Speech );
			}
		}

		private static void EventSink_Speech( SpeechEventArgs e )
		{
			if( e.Mobile != null && !e.Mobile.Deleted && e.Mobile.Alive )
			{
				if( Insensitive.Contains( e.Speech, "trick or treat" ) )
				{
					e.Mobile.Target = new TrickOrTreatTarget();

					e.Mobile.SendLocalizedMessage( 1076764 );  /* Pick someone to Trick or Treat. */
				}
			}
		}

		public virtual void DeleteNaughtyTwin()
		{
		}

		private class TrickOrTreatTarget : Target
		{
			public TrickOrTreatTarget()
				: base( 15, false, TargetFlags.None )
			{
			}

			protected override void OnTarget( Mobile from, object targ )
			{
				if( !( targ is Mobile ) )
				{
					from.SendLocalizedMessage( 1076781 ); /* There is little chance of getting candy from that! */
					return;
				}
				if( !( targ is BaseVendor ) || ( ( BaseVendor )targ ).Deleted )
				{
					from.SendLocalizedMessage( 1076765 ); /* That doesn't look friendly. */
					return;
				}

				DateTime now = DateTime.Now;

				BaseVendor m_Begged = targ as BaseVendor;

				if( m_Begged.NextTrickOrTreat > now )
				{
					from.SendLocalizedMessage( 1076767 ); /* That doesn't appear to have any more candy. */
					return;
				}

				m_Begged.NextTrickOrTreat = now + TimeSpan.FromMinutes( Utility.RandomMinMax( 5, 10 ) );

				if( from.Backpack != null && !from.Backpack.Deleted )
				{
					if( Utility.RandomDouble() > .10 )
					{
						switch( Utility.Random( 3 ) )
						{
							case 0: m_Begged.Say( 1076768 ); break; /* Oooooh, aren't you cute! */
							case 1: m_Begged.Say( 1076779 ); break; /* All right...This better not spoil your dinner! */
							case 2: m_Begged.Say( 1076778 ); break; /* Here you go! Enjoy! */
							default: break;
						}

						if( Utility.RandomDouble() <= .01 && from.Skills.Begging.Value >= 100 )
						{
							
							from.AddToBackpack( HolidaySettings.RandomGMBeggerItem );

							from.SendLocalizedMessage( 1076777 ); /* You receive a special treat! */
						}
						else
						{
							from.AddToBackpack( HolidaySettings.RandomTreat );

							from.SendLocalizedMessage( 1076769 );   /* You receive some candy. */
						}
					}
					else
					{
						m_Begged.Say( 1076770 ); /* TRICK! */

						MakeTwin( from );
					}
				}
			}
		}

		public static void MakeTwin( Mobile from )
		{
			List<Item> m_Items = new List<Item>();

			Mobile twin = new NaughtyTwin( from );

			if( twin != null && !twin.Deleted )
			{
				foreach( Item item in from.Items )
				{
					if( item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank )
					{
						m_Items.Add( item );
					}
				}

				for( int i = 0; i < m_Items.Count; i++ )
				{
					twin.AddItem( Mobile.LiftItemDupe( m_Items[ i ], 1 ) ); // TODO: Clone weapon/armor attributes
				}

				twin.Hue = from.Hue;
				twin.BodyValue = from.BodyValue;

				Point3D point = RandomPointOneAway( from.X, from.Y, from.Map );

				twin.MoveToWorld( from.Location, from.Map );

				Timer.DelayCall( TimeSpan.FromSeconds( 5 ), new TimerCallback( twin.Delete ) );
			}
		}

		public static Point3D RandomPointOneAway( int x, int y, Map map )
		{
			Point3D loc = new Point3D( x + Utility.RandomMinMax( -1, 1 ), y + Utility.RandomMinMax( -1, 1 ), 0 );

			loc.Z = map.GetAverageZ( loc.X, loc.Y );

			return loc;
		}
	}

	
	public class NaughtyTwin : BaseCreature
	{
		private Mobile m_From;

		private static Point3D[] Felucca_Locations =
		{
			new Point3D( 4467, 1283, 5 ), // Moonglow
			new Point3D( 1336, 1997, 5 ), // Britain
			new Point3D( 1499, 3771, 5 ), // Jhelom
			new Point3D(  771,  752, 5 ), // Yew
			new Point3D( 2701,  692, 5 ), // Minoc
			new Point3D( 1828, 2948,-20), // Trinsic
			new Point3D(  643, 2067, 5 ), // Skara Brae
					/* Dynamic Z for Magincia to support both old and new maps. */
			new Point3D( 3563, 2139, Map.Trammel.GetAverageZ( 3563, 2139 ) ), // (New) Magincia
		};

		private static Point3D[] Malas_Locations =
		{
			new Point3D(1015, 527, -65), // Luna
			new Point3D(1997, 1386, -85) // Umbra
		};

		private static Point3D[] Ilshenar_Locations =
		{
			new Point3D( 1215,  467, -13 ), // Compassion
			new Point3D(  722, 1366, -60 ), // Honesty
			new Point3D(  744,  724, -28 ), // Honor
			new Point3D(  281, 1016,   0 ), // Humility
			new Point3D(  987, 1011, -32 ), // Justice
			new Point3D( 1174, 1286, -30 ), // Sacrifice
			new Point3D( 1532, 1340, - 3 ), // Spirituality
			new Point3D(  528,  216, -45 ), // Valor
			new Point3D( 1721,  218,  96 )  // Chaos
		};

		private static Point3D[] Tokuno_Locations =
		{
			new Point3D( 1169,  998, 41 ), // Isamu-Jima
			new Point3D(  802, 1204, 25 ), // Makoto-Jima
			new Point3D(  270,  628, 15 )  // Homare-Jima
		};

		[Constructable]
		public NaughtyTwin()
			: this( null )
		{
		}

		public NaughtyTwin( Mobile from )
			: base( AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4 )
		{
			if( from != null )
			{
				Body = from.Body;

				m_From = from;
				Name = String.Format( "{0}\'s Naughty Twin", from.Name );

				switch( Utility.Random( 4 ) )
				{
					case 0: Timer.DelayCall( TimeSpan.FromSeconds( 1 ), new TimerCallback( StealCandycallBack ) ); break;
					case 1: Timer.DelayCall( TimeSpan.FromSeconds( 1 ), new TimerCallback( TeleportBeggar ) ); break;
					case 2: Timer.DelayCall( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 1 ), 5, new TimerCallback( Bleeding ) ); break;
					default: Timer.DelayCall( TimeSpan.FromSeconds( 1 ), new TimerCallback( SolidHueMobile ) ); break;
				}
			}
		}

		public virtual void RemoveHueMod()
		{
			m_From.SolidHueOverride = -1;
		}

		public virtual void SolidHueMobile()
		{
			m_From.SolidHueOverride = Utility.RandomMinMax( 2501, 2644 );

			Timer.DelayCall( TimeSpan.FromSeconds( 10 ), new TimerCallback( RemoveHueMod ) );
		}

		public virtual void StealCandycallBack()
		{
			Item item = null;
			m_From.SendLocalizedMessage( 1113967 ); /* Your naughty twin steals some of your candy. */

			item = m_From.Backpack.FindItemByType( typeof( WrappedCandy ) );

			if( item == null )
			{
				item = m_From.Backpack.FindItemByType( typeof( Lollipops ) );
			}
			if( item == null )
			{
				item = m_From.Backpack.FindItemByType( typeof( NougatSwirl ) );
			}
			if( item == null )
			{
				item = m_From.Backpack.FindItemByType( typeof( Taffy ) );
			}
			if( item == null )
			{
				item = m_From.Backpack.FindItemByType( typeof( JellyBeans ) );
			}
			if( item != null && !item.Deleted )
			{
				item.Delete();
			}
		}

		public virtual void TeleportBeggar()
		{
			m_From.SendLocalizedMessage( 1113972 ); /* Your naughty twin teleports you away with a naughty laugh! */

			m_From.MoveToWorld( RandomMoongate(), m_From.Map );
		}

		public virtual Point3D RandomMoongate()
		{
			Map map = m_From.Map;

			switch( m_From.Map.Name )
			{
				case "Ilshenar": return Ilshenar_Locations[ Utility.Random( Ilshenar_Locations.Length ) ];
				case "Malas": return Malas_Locations[ Utility.Random( Malas_Locations.Length ) ];
				case "Tokuno": return Tokuno_Locations[ Utility.Random( Tokuno_Locations.Length ) ];
				default: return Felucca_Locations[ Utility.Random( Felucca_Locations.Length ) ];
			}
		}

		public virtual void Bleeding()
		{
			if( m_From != null && m_From.Map != null && !m_From.Deleted && m_From.Alive && m_From.Map != Map.Internal )
			{
				if( m_From.Location != Point3D.Zero )
				{
					int amount = Utility.RandomMinMax( 0, 7 );

					for( int i = 0; i < amount; i++ )
					{
						Point3D loc = TrickOrTreat.RandomPointOneAway( m_From.X, m_From.Y, m_From.Map );

						if( m_From.Map.CanFit( loc, 1 ) )
						{
							continue;
						}
						else
						{
							loc = m_From.Location;
						}

						Blood blood = new Blood( Utility.RandomMinMax( 0x122C, 0x122F ) );

						blood.MoveToWorld( loc, m_From.Map );
					}
				}
			}
		}

		public NaughtyTwin( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( ( int )0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}