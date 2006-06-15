using System;
using System.Collections;
using Server;
using Server.Network;

namespace Server.Items
{
	[FlipableAttribute( 0x100A/*East*/, 0x100B/*South*/ )]
	public class ArcheryButte : AddonComponent
	{
		private double m_MinSkill;
		private double m_MaxSkill;

		private int m_Arrows, m_Bolts;

		private DateTime m_LastUse;

		[CommandProperty( AccessLevel.GameMaster )]
		public double MinSkill
		{
			get{ return m_MinSkill; }
			set{ m_MinSkill = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public double MaxSkill
		{
			get{ return m_MaxSkill; }
			set{ m_MaxSkill = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime LastUse
		{
			get{ return m_LastUse; }
			set{ m_LastUse = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool FacingEast
		{
			get{ return ( ItemID == 0x100A ); }
			set{ ItemID = value ? 0x100A : 0x100B; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Arrows
		{
			get{ return m_Arrows; }
			set{ m_Arrows = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Bolts
		{
			get{ return m_Bolts; }
			set{ m_Bolts = value; }
		}

		[Constructable]
		public ArcheryButte() : this( 0x100A )
		{
		}

		public ArcheryButte( int itemID ) : base( itemID )
		{
			m_MinSkill = -25.0;
			m_MaxSkill = +25.0;
		}

		public ArcheryButte( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( (m_Arrows > 0 || m_Bolts > 0) && from.InRange( GetWorldLocation(), 1 ) )
				Gather( from );
			else
				Fire( from );
		}

		public void Gather( Mobile from )
		{
			from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500592 ); // You gather the arrows and bolts.

			if ( m_Arrows > 0 )
				from.AddToBackpack( new Arrow( m_Arrows ) );

			if ( m_Bolts > 0 )
				from.AddToBackpack( new Bolt( m_Bolts ) );

			m_Arrows = 0;
			m_Bolts = 0;

			m_Entries = null;
		}

		private static TimeSpan UseDelay = TimeSpan.FromSeconds( 2.0 );

		private class ScoreEntry
		{
			private int m_Total;
			private int m_Count;

			public int Total{ get{ return m_Total; } set{ m_Total = value; } }
			public int Count{ get{ return m_Count; } set{ m_Count = value; } }

			public void Record( int score )
			{
				m_Total += score;
				m_Count += 1;
			}

			public ScoreEntry()
			{
			}
		}

		private Hashtable m_Entries;

		private ScoreEntry GetEntryFor( Mobile from )
		{
			if ( m_Entries == null )
				m_Entries = new Hashtable();

			ScoreEntry e = (ScoreEntry)m_Entries[from];

			if ( e == null )
				m_Entries[from] = e = new ScoreEntry();

			return e;
		}

		public void Fire( Mobile from )
		{
			BaseRanged bow = from.Weapon as BaseRanged;

			if ( bow == null )
			{
				SendLocalizedMessageTo( from, 500593 ); // You must practice with ranged weapons on this.
				return;
			}

			if ( DateTime.Now < (m_LastUse + UseDelay) )
				return;

			Point3D worldLoc = GetWorldLocation();

			if ( FacingEast ? from.X <= worldLoc.X : from.Y <= worldLoc.Y )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500596 ); // You would do better to stand in front of the archery butte.
				return;
			}

			if ( FacingEast ? from.Y != worldLoc.Y : from.X != worldLoc.X )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500597 ); // You aren't properly lined up with the archery butte to get an accurate shot.
				return;
			}

			if ( !from.InRange( worldLoc, 6 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500598 ); // You are too far away from the archery butte to get an accurate shot.
				return;
			}
			else if ( from.InRange( worldLoc, 4 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500599 ); // You are too close to the target.
				return;
			}

			Container pack = from.Backpack;
			Type ammoType = bow.AmmoType;

			bool isArrow = ( ammoType == typeof( Arrow ) );
			bool isBolt = ( ammoType == typeof( Bolt ) );
			bool isKnown = ( isArrow || isBolt );

			if ( pack == null || !pack.ConsumeTotal( ammoType, 1 ) )
			{
				if ( isArrow )
					from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500594 ); // You do not have any arrows with which to practice.
				else if ( isBolt )
					from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 500595 ); // You do not have any crossbow bolts with which to practice.
				else
					SendLocalizedMessageTo( from, 500593 ); // You must practice with ranged weapons on this.

				return;
			}

			m_LastUse = DateTime.Now;

			from.Direction = from.GetDirectionTo( GetWorldLocation() );
			bow.PlaySwingAnimation( from );
			from.MovingEffect( this, bow.EffectID, 18, 1, false, false );

			ScoreEntry se = GetEntryFor( from );

			if ( !from.CheckSkill( bow.Skill, m_MinSkill, m_MaxSkill ) )
			{
				from.PlaySound( bow.MissSound );

				PublicOverheadMessage( MessageType.Regular, 0x3B2, 500604, from.Name ); // You miss the target altogether.

				se.Record( 0 );

				if ( se.Count == 1 )
					PublicOverheadMessage( MessageType.Regular, 0x3B2, 1062719, se.Total.ToString() );
				else
					PublicOverheadMessage( MessageType.Regular, 0x3B2, 1042683, String.Format( "{0}\t{1}", se.Total, se.Count ) );

				return;
			}

			Effects.PlaySound( Location, Map, 0x2B1 );

			double rand = Utility.RandomDouble();

			int area, score, splitScore;

			if ( 0.10 > rand )
			{
				area = 0; // bullseye
				score = 50;
				splitScore = 100;
			}
			else if ( 0.25 > rand )
			{
				area = 1; // inner ring
				score = 10;
				splitScore = 20;
			}
			else if ( 0.50 > rand )
			{
				area = 2; // middle ring
				score = 5;
				splitScore = 15;
			}
			else
			{
				area = 3; // outer ring
				score = 2;
				splitScore = 5;
			}

			bool split = ( isKnown && ((m_Arrows + m_Bolts) * 0.02) > Utility.RandomDouble() );

			if ( split )
			{
				PublicOverheadMessage( MessageType.Regular, 0x3B2, 1010027 + (isArrow ? 0 : 4) + area, from.Name );
			}
			else
			{
				PublicOverheadMessage( MessageType.Regular, 0x3B2, 1010035 + area, from.Name );

				if ( isArrow )
					++m_Arrows;
				else if ( isBolt )
					++m_Bolts;
			}

			se.Record( split ? splitScore : score );

			if ( se.Count == 1 )
				PublicOverheadMessage( MessageType.Regular, 0x3B2, 1062719, se.Total.ToString() );
			else
				PublicOverheadMessage( MessageType.Regular, 0x3B2, 1042683, String.Format( "{0}\t{1}", se.Total, se.Count ) );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( m_MinSkill );
			writer.Write( m_MaxSkill );
			writer.Write( m_Arrows );
			writer.Write( m_Bolts );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_MinSkill = reader.ReadDouble();
					m_MaxSkill = reader.ReadDouble();
					m_Arrows = reader.ReadInt();
					m_Bolts = reader.ReadInt();

					if ( m_MinSkill == 0.0 && m_MaxSkill == 30.0 )
					{
						m_MinSkill = -25.0;
						m_MaxSkill = +25.0;
					}

					break;
				}
			}
		}
	}

	public class ArcheryButteAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcheryButteDeed(); } }

		[Constructable]
		public ArcheryButteAddon()
		{
			AddComponent( new ArcheryButte( 0x100A ), 0, 0, 0 );
		}

		public ArcheryButteAddon( Serial serial ) : base( serial )
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
	}

	public class ArcheryButteDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcheryButteAddon(); } }
		public override int LabelNumber{ get{ return 1024106; } } // archery butte

		[Constructable]
		public ArcheryButteDeed()
		{
		}

		public ArcheryButteDeed( Serial serial ) : base( serial )
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
	}
}