using System;
using System.Text;
using System.Collections;
using Server.Network;
using Server.Targeting;
using Server.Spells;

namespace Server.Items
{
	public enum WandEffect
	{
		Clumsiness,
		Identification,
		Healing,
		Feeblemindedness,
		Weakness,
		MagicArrow,
		Harming,
		Fireball,
		GreaterHealing,
		Lightning,
		ManaDraining
	}

	public abstract class BaseWand : BaseBashing
	{
		private WandEffect m_WandEffect;
		private int m_Charges;

		public virtual TimeSpan GetUseDelay{ get{ return TimeSpan.FromSeconds( 4.0 ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public WandEffect Effect
		{
			get{ return m_WandEffect; }
			set{ m_WandEffect = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Charges
		{
			get{ return m_Charges; }
			set{ m_Charges = value; InvalidateProperties(); }
		}

		public BaseWand( WandEffect effect, int minCharges, int maxCharges ) : base( Utility.RandomList( 0xDF2, 0xDF3, 0xDF4, 0xDF5 ) )
		{
			Weight = 1.0;
			Effect = effect;
			Charges = Utility.RandomMinMax( minCharges, maxCharges );
		}

		public void ConsumeCharge( Mobile from )
		{
			--Charges;

			if ( Charges == 0 )
				from.SendLocalizedMessage( 1019073 ); // This item is out of charges.

			ApplyDelayTo( from );
		}

		public BaseWand( Serial serial ) : base( serial )
		{
		}

		public virtual void ApplyDelayTo( Mobile from )
		{
			from.BeginAction( typeof( BaseWand ) );
			Timer.DelayCall( GetUseDelay, new TimerStateCallback( ReleaseWandLock_Callback ), from );
		}

		public virtual void ReleaseWandLock_Callback( object state )
		{
			((Mobile)state).EndAction( typeof( BaseWand ) );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.CanBeginAction( typeof( BaseWand ) ) )
				return;

			if ( Parent == from )
			{
				if ( Charges > 0 )
					OnWandUse( from );
				else
					from.SendLocalizedMessage( 1019073 ); // This item is out of charges.
			}
			else
			{
				from.SendLocalizedMessage( 502641 ); // You must equip this item to use it.
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_WandEffect );
			writer.Write( (int) m_Charges );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_WandEffect = (WandEffect)reader.ReadInt();
					m_Charges = (int)reader.ReadInt();

					break;
				}
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			switch ( m_WandEffect )
			{
				case WandEffect.Clumsiness:			list.Add( 1017326, m_Charges.ToString() ); break; // clumsiness charges: ~1_val~
				case WandEffect.Identification:		list.Add( 1017350, m_Charges.ToString() ); break; // identification charges: ~1_val~
				case WandEffect.Healing:			list.Add( 1017329, m_Charges.ToString() ); break; // healing charges: ~1_val~
				case WandEffect.Feeblemindedness:	list.Add( 1017327, m_Charges.ToString() ); break; // feeblemind charges: ~1_val~
				case WandEffect.Weakness:			list.Add( 1017328, m_Charges.ToString() ); break; // weakness charges: ~1_val~
				case WandEffect.MagicArrow:			list.Add( 1060492, m_Charges.ToString() ); break; // magic arrow charges: ~1_val~
				case WandEffect.Harming:			list.Add( 1017334, m_Charges.ToString() ); break; // harm charges: ~1_val~
				case WandEffect.Fireball:			list.Add( 1060487, m_Charges.ToString() ); break; // fireball charges: ~1_val~
				case WandEffect.GreaterHealing:		list.Add( 1017330, m_Charges.ToString() ); break; // greater healing charges: ~1_val~
				case WandEffect.Lightning:			list.Add( 1060491, m_Charges.ToString() ); break; // lightning charges: ~1_val~
				case WandEffect.ManaDraining:		list.Add( 1017339, m_Charges.ToString() ); break; // mana drain charges: ~1_val~
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			ArrayList attrs = new ArrayList();

			if ( DisplayLootType )
			{
				if ( LootType == LootType.Blessed )
					attrs.Add( new EquipInfoAttribute( 1038021 ) ); // blessed
				else if ( LootType == LootType.Cursed )
					attrs.Add( new EquipInfoAttribute( 1049643 ) ); // cursed
			}

			if ( !Identified )
			{
				attrs.Add( new EquipInfoAttribute( 1038000 ) ); // Unidentified
			}
			else
			{
				int num = 0;

				switch ( m_WandEffect )
				{
					case WandEffect.Clumsiness:			num = 3002011; break;
					case WandEffect.Identification:		num = 1044063; break;
					case WandEffect.Healing:			num = 3002014; break;
					case WandEffect.Feeblemindedness:	num = 3002013; break;
					case WandEffect.Weakness:			num = 3002018; break;
					case WandEffect.MagicArrow:			num = 3002015; break;
					case WandEffect.Harming:			num = 3002022; break;
					case WandEffect.Fireball:			num = 3002028; break;
					case WandEffect.GreaterHealing:		num = 3002039; break;
					case WandEffect.Lightning:			num = 3002040; break;
					case WandEffect.ManaDraining:		num = 3002041; break;
				}

				if ( num > 0 )
					attrs.Add( new EquipInfoAttribute( num, m_Charges ) );
			}

			int number;

			if ( Name == null )
			{
				number = 1017085;
			}
			else
			{
				this.LabelTo( from, Name );
				number = 1041000;
			}

			if ( attrs.Count == 0 && Crafter == null && Name != null )
				return;

			EquipmentInfo eqInfo = new EquipmentInfo( number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray( typeof( EquipInfoAttribute ) ) );

			from.Send( new DisplayEquipmentInfo( this, eqInfo ) );
		}

		public void Cast( Spell spell )
		{
			bool m = Movable;

			Movable = false;
			spell.Cast();
			Movable = m;
		}

		public virtual void OnWandUse( Mobile from )
		{
			from.Target = new WandTarget( this );
		}

		public virtual void DoWandTarget( Mobile from, object o )
		{
			if ( Deleted || Charges <= 0 || Parent != from || o is StaticTarget || o is LandTarget )
				return;

			if ( OnWandTarget( from, o ) )
				ConsumeCharge( from );
		}

		public virtual bool OnWandTarget( Mobile from, object o )
		{
			return true;
		}
	}
}