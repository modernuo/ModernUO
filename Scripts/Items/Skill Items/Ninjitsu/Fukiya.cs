using System;
using Server;
using Server.Targeting;
using System.Collections;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Items
{
	[FlipableAttribute( 0x27AA, 0x27F5 )]
	public class Fukiya : Item, INinjaWeapon
	{
		public virtual int WrongAmmoMessage { get { return 1063329; } } //You can only load fukiya darts
		public virtual int NoFreeHandMessage { get { return 1063327; } } //You must have a free hand to use a fukiya.
		public virtual int EmptyWeaponMessage { get { return 1063325; } } //You have no fukiya darts!
		public virtual int RecentlyUsedMessage { get { return 1063326; } } //You are already using that fukiya.
		public virtual int FullWeaponMessage { get { return 1063330; } } //You can only load fukiya darts

		public virtual int WeaponMinRange { get { return 0; } }
		public virtual int WeaponMaxRange { get { return 6; } }

		public virtual int WeaponDamage { get { return Utility.RandomMinMax(4, 6); } }

		public  Type AmmoType{ get { return typeof(FukiyaDarts); } }

		private int m_UsesRemaining;
		private Poison m_Poison;
		private int m_PoisonCharges;

		[CommandProperty( AccessLevel.GameMaster )]
		public int UsesRemaining
		{
			get { return m_UsesRemaining; }
			set { m_UsesRemaining = value; InvalidateProperties();  }
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
		public Fukiya() : base( 0x27AA )
		{
			Weight = 4.0;
			Layer = Layer.OneHanded;
		}

		public Fukiya( Serial serial ) : base( serial )
		{
		}

		public void AttackAnimation(Mobile from, Mobile to)
		{
			if (from.Body.IsHuman && !from.Mounted)
			{
				from.Animate(33, 2, 1, true, true, 0);
			}

			from.PlaySound(0x223);
			from.MovingEffect(to, 0x2804, 5, 0, false, false);
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1060584, m_UsesRemaining.ToString() ); // uses remaining: ~1_val~

			if ( m_Poison != null && m_PoisonCharges > 0 )
				list.Add( 1062412 + m_Poison.Level, m_PoisonCharges.ToString() );
		}

		public override void OnDoubleClick( Mobile from )
		{
			NinjaWeapon.AttemptShoot((PlayerMobile)from, this);
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( IsChildOf( from ) )
			{
				list.Add(new NinjaWeapon.LoadEntry(this, 6224));
				list.Add(new NinjaWeapon.UnloadEntry(this, 6225));
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