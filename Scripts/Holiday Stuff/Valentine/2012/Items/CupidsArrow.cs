using System;
using Server;
using Server.Targeting;

namespace Server.Items
{
	public class CupidsArrow : Item
	{
		// TODO: Check messages

		public override int LabelNumber { get { return 1152270; } } // Cupid's Arrow 2012

		private string m_From;
		private string m_To;

		[CommandProperty( AccessLevel.GameMaster )]
		public string From
		{
			get { return m_From; }
			set { m_From = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string To
		{
			get { return m_To; }
			set { m_To = value; InvalidateProperties(); }
		}

		public bool IsSigned
		{
			get { return ( m_From != null && m_To != null ); }
		}

		[Constructable]
		public CupidsArrow()
			: base( 0x4F7F )
		{
			LootType = LootType.Blessed;
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			base.AddNameProperty( list );

			if ( IsSigned )
				list.Add( 1152273, String.Format( "{0}\t{1}", m_From, m_To ) ); // ~1_val~ is madly in love with ~2_val~
		}

		public static bool CheckSeason( Mobile from )
		{
			if ( DateTime.UtcNow.Month == 2 )
				return true;

			from.SendLocalizedMessage( 1152318 ); // You may not use this item out of season.
			return false;
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( IsSigned )
				LabelTo( from, 1152273, String.Format( "{0}\t{1}", m_From, m_To ) ); // ~1_val~ is madly in love with ~2_val~
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( IsSigned || !CheckSeason( from ) )
				return;

			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1080063 ); // This must be in your backpack to use it.
				return;
			}

			from.BeginTarget( 10, false, TargetFlags.None, new TargetCallback( OnTarget ) );
			from.SendMessage( "Who do you wish to use this on?" );
		}

		private void OnTarget( Mobile from, object targeted )
		{
			if ( IsSigned || !IsChildOf( from.Backpack ) )
				return;

			if ( targeted is Mobile )
			{
				Mobile m = (Mobile)targeted;

				if ( !m.Alive )
				{
					from.SendLocalizedMessage( 1152269 ); // That target is dead and even Cupid's arrow won't make them love you.
					return;
				}

				m_From = from.Name;
				m_To = m.Name;

				InvalidateProperties();

				from.SendMessage( "You inscribe the arrow." );
			}
			else
			{
				from.SendMessage( "That is not a person." );
			}
		}

		public CupidsArrow( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );

			writer.Write( m_From );
			writer.Write( m_To );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_From = Utility.Intern( reader.ReadString() );
			m_To = Utility.Intern( reader.ReadString() );
		}
	}
}
