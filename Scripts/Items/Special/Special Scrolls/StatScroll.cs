using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
	public class StatCapScroll : Item
	{
		private int m_Value;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Value
		{
			get
			{
				return m_Value;
			}
		}

		[Constructable]
		public StatCapScroll( int value ) : base( 0x14F0 )
		{
			Hue = 0x481;
			Weight = 1.0;

			LootType = LootType.Cursed;

			m_Value = value;
		}

		public StatCapScroll( Serial serial ) : base( serial )
		{
		}

		public override void AddNameProperty(ObjectPropertyList list)
		{
			if ( m_Value == 230 )
				list.Add( 1049463, "#1049476" ); // a wonderous scroll of ~1_type~ (+5 Maximum Stats)
			else if ( m_Value == 235 )
				list.Add( 1049464, "#1049476" ); // an exalted scroll of ~1_type~ (+10 Maximum Stats)
			else if ( m_Value == 240 )
				list.Add( 1049465, "#1049476" ); // a mythical scroll of ~1_type~ (+15 Maximum Stats)
			else if ( m_Value == 245 )
				list.Add( 1049466, "#1049476" ); // a legendary scroll of ~1_type~ (+20 Maximum Stats)
			else if ( m_Value == 250 )
				list.Add( 1049467, "#1049476" ); // an ultimate scroll of ~1_type~ (+25 Maximum Stats)
			else
				list.Add( "a scroll of power ({0}{1} Maximum Stats)", (m_Value - 225) >= 0 ? "+" : "", m_Value - 225 );
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Value == 230 )
				base.LabelTo( from, 1049463, "#1049476" ); // a wonderous scroll of ~1_type~ (+5 Maximum Stats)
			else if ( m_Value == 235 )
				base.LabelTo( from, 1049464, "#1049476" ); // an exalted scroll of ~1_type~ (+10 Maximum Stats)
			else if ( m_Value == 240 )
				base.LabelTo( from, 1049465, "#1049476" ); // a mythical scroll of ~1_type~ (+15 Maximum Stats)
			else if ( m_Value == 245 )
				base.LabelTo( from, 1049466, "#1049476" ); // a legendary scroll of ~1_type~ (+20 Maximum Stats)
			else if ( m_Value == 250 )
				base.LabelTo( from, 1049467, "#1049476" ); // an ultimate scroll of ~1_type~ (+25 Maximum Stats)
			else
				base.LabelTo( from, "a scroll of power ({0}{1} Maximum Stats)", (m_Value - 225) >= 0 ? "+" : "", m_Value - 225 );
		}

		public void Use( Mobile from, bool firstStage )
		{
			if ( Deleted )
				return;

			if ( IsChildOf( from.Backpack ) )
			{
				if ( from.StatCap >= m_Value )
				{
					from.SendLocalizedMessage( 1049510 ); // Your stats are too high for this power scroll.
				}
				else
				{
					if ( firstStage )
					{
						from.CloseGump( typeof( StatCapScroll.InternalGump ) );
						from.CloseGump( typeof( PowerScroll.InternalGump ) );
						from.SendGump( new InternalGump( from, this ) );
					}
					else
					{
						from.SendLocalizedMessage( 1049512 ); // You feel a surge of magic as the scroll enhances your powers!

						from.StatCap = m_Value;

						Effects.SendLocationParticles( EffectItem.Create( from.Location, from.Map, EffectItem.DefaultDuration ), 0, 0, 0, 0, 0, 5060, 0 );
						Effects.PlaySound( from.Location, from.Map, 0x243 );

						Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 6, from.Y - 6, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );
						Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 4, from.Y - 6, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );
						Effects.SendMovingParticles( new Entity( Serial.Zero, new Point3D( from.X - 6, from.Y - 4, from.Z + 15 ), from.Map ), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100 );

						Effects.SendTargetParticles( from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100 );

						Delete();
					}
				}
			}
			else
			{
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			Use( from, true );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_Value );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Value = reader.ReadInt();

					break;
				}
			}

			if ( LootType != LootType.Cursed )
				LootType = LootType.Cursed;

			if ( Insured )
				Insured = false;
		}

		public class InternalGump : Gump
		{
			private Mobile m_Mobile;
			private StatCapScroll m_Scroll;

			public InternalGump( Mobile mobile, StatCapScroll scroll ) : base( 25, 50 )
			{
				m_Mobile = mobile;
				m_Scroll = scroll;

				AddPage( 0 );

				AddBackground( 25, 10, 420, 200, 5054 );

				AddImageTiled( 33, 20, 401, 181, 2624 );
				AddAlphaRegion( 33, 20, 401, 181 );

				AddHtmlLocalized( 40, 48, 387, 100, 1049469, true, true ); /* Using a scroll increases the maximum amount of a specific skill or your maximum statistics.
																			* When used, the effect is not immediately seen without a gain of points with that skill or statistics.
																			* You can view your maximum skill values in your skills window.
																			* You can view your maximum statistic value in your statistics window.
																			*/
				AddHtmlLocalized( 125, 148, 200, 20, 1049478, 0xFFFFFF, false, false ); // Do you wish to use this scroll?

				AddButton( 100, 172, 4005, 4007, 1, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 135, 172, 120, 20, 1046362, 0xFFFFFF, false, false ); // Yes

				AddButton( 275, 172, 4005, 4007, 0, GumpButtonType.Reply, 0 );
				AddHtmlLocalized( 310, 172, 120, 20, 1046363, 0xFFFFFF, false, false ); // No

				int value = scroll.m_Value;

				if ( value == 230 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049458, 0xFFFFFF, false, false ); // Wonderous Scroll (+5 Maximum Stats):
				else if ( value == 235 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049459, 0xFFFFFF, false, false ); // Exalted Scroll (+10 Maximum Stats):
				else if ( value == 240 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049460, 0xFFFFFF, false, false ); // Mythical Scroll (+15 Maximum Stats):
				else if ( value == 245 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049461, 0xFFFFFF, false, false ); // Legendary Scroll (+20 Maximum Stats):
				else if ( value == 250 )
					AddHtmlLocalized( 40, 20, 260, 20, 1049462, 0xFFFFFF, false, false ); // Ultimate Scroll (+25 Maximum Stats):
				else
					AddHtml( 40, 20, 260, 20, String.Format( "<basefont color=#FFFFFF>Power Scroll ({0}{1} Maximum Stats):</basefont>", (value - 225) >= 0 ? "+" : "", value - 225), false, false );

				AddHtmlLocalized( 310, 20, 120, 20, 1038019, 0xFFFFFF, false, false ); // Power
			}

			public override void OnResponse( NetState state, RelayInfo info )
			{
				if ( info.ButtonID == 1 )
					m_Scroll.Use( m_Mobile, false );
			}
		}
	}
}