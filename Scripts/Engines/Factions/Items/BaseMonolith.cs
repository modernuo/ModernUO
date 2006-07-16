using System;
using System.Collections.Generic;

namespace Server.Factions
{
	public abstract class BaseMonolith : BaseSystemController
	{
		private Town m_Town;
		private Faction m_Faction;
		private Sigil m_Sigil;

		[CommandProperty( AccessLevel.Counselor, AccessLevel.Administrator )]
		public Sigil Sigil
		{
			get{ return m_Sigil; }
			set
			{
				if ( m_Sigil == value )
					return;

				m_Sigil = value;

				if ( m_Sigil != null && m_Sigil.LastMonolith != null && m_Sigil.LastMonolith != this && m_Sigil.LastMonolith.Sigil == m_Sigil )
					m_Sigil.LastMonolith.Sigil = null;

				if ( m_Sigil != null )
					m_Sigil.LastMonolith = this;

				UpdateSigil();
			}
		}

		[CommandProperty( AccessLevel.Counselor, AccessLevel.Administrator )]
		public Town Town
		{
			get{ return m_Town; }
			set
			{
				m_Town = value;
				OnTownChanged();
			}
		}

		[CommandProperty( AccessLevel.Counselor, AccessLevel.Administrator )]
		public Faction Faction
		{
			get{ return m_Faction; }
			set
			{
				m_Faction = value;
				Hue = ( m_Faction == null ? 0 : m_Faction.Definition.HuePrimary );
			}
		}

		public override void OnLocationChange( Point3D oldLocation )
		{
			base.OnLocationChange( oldLocation );
			UpdateSigil();
		}

		public override void OnMapChange()
		{
			base.OnMapChange();
			UpdateSigil();
		}

		public virtual void UpdateSigil()
		{
			if ( m_Sigil == null || m_Sigil.Deleted )
				return;

			m_Sigil.MoveToWorld( new Point3D( X, Y, Z + 18 ), Map );
		}

		public virtual void OnTownChanged()
		{
		}

		public BaseMonolith( Town town, Faction faction ) : base( 0x1183 )
		{
			Movable = false;
			Town = town;
			Faction = faction;
			m_Monoliths.Add( this );
		}

		public BaseMonolith( Serial serial ) : base( serial )
		{
			m_Monoliths.Add( this );
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();
			m_Monoliths.Remove( this );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Town.WriteReference( writer, m_Town );
			Faction.WriteReference( writer, m_Faction );

			writer.Write( (Item) m_Sigil );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					Town = Town.ReadReference( reader );
					Faction = Faction.ReadReference( reader );
					m_Sigil = reader.ReadItem() as Sigil;
					break;
				}
			}
		}

		private static List<BaseMonolith> m_Monoliths = new List<BaseMonolith>();

		public static List<BaseMonolith> Monoliths
		{
			get{ return m_Monoliths; }
			set{ m_Monoliths = value; }
		}
	}
}