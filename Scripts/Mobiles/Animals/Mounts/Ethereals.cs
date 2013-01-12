using System;
using Server.Mobiles;
using Server.Items;
using Server.Spells;
using Server.Engines.VeteranRewards;

namespace Server.Mobiles
{
	public class EtherealMount : Item, IMount, IMountItem, Engines.VeteranRewards.IRewardItem
	{
		private int m_MountedID;
		private int m_RegularID;
		private Mobile m_Rider;
		private bool m_IsRewardItem;
		private bool m_IsDonationItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get { return m_IsRewardItem; }
			set { m_IsRewardItem = value; }
		}

		public override double DefaultWeight
		{
			get { return 1.0; }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public bool IsDonationItem
		{
			get { return m_IsDonationItem; }
			set { m_IsDonationItem = value; InvalidateProperties(); }
		}

		[Constructable]
		public EtherealMount( int itemID, int mountID )
			: base( itemID )
		{
			m_MountedID = mountID;
			m_RegularID = itemID;
			m_Rider = null;

			Layer = Layer.Invalid;

			LootType = LootType.Blessed;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if( m_IsDonationItem )
			{
				list.Add( "Donation Ethereal" );
				list.Add( "7.5 sec slower cast time if not a 9mo. Veteran" );
			}
			if( Core.ML && m_IsRewardItem )
				list.Add( RewardSystem.GetRewardYearLabel( this, new object[] { } ) ); // X Year Veteran Reward
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MountedID
		{
			get
			{
				return m_MountedID;
			}
			set
			{
				if( m_MountedID != value )
				{
					m_MountedID = value;

					if( m_Rider != null )
						ItemID = value;
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int RegularID
		{
			get
			{
				return m_RegularID;
			}
			set
			{
				if( m_RegularID != value )
				{
					m_RegularID = value;

					if( m_Rider == null )
						ItemID = value;
				}
			}
		}

		public EtherealMount( Serial serial )
			: base( serial )
		{
		}

		public override bool DisplayLootType { get { return false; } }

		public virtual int FollowerSlots { get { return 1; } }

		public void RemoveFollowers()
		{
			if( m_Rider != null )
				m_Rider.Followers -= FollowerSlots;

			if( m_Rider != null && m_Rider.Followers < 0 )
				m_Rider.Followers = 0;
		}

		public void AddFollowers()
		{
			if( m_Rider != null )
				m_Rider.Followers += FollowerSlots;
		}

		public virtual bool Validate( Mobile from )
		{
			if( Parent == null )
			{
				from.SayTo( from, 1010095 ); // This must be on your person to use.
				return false;
			}
			else if( m_IsRewardItem && !RewardSystem.CheckIsUsableBy( from, this, null ) )
			{
				// CheckIsUsableBy sends the message
				return false;
			}
			else if( !BaseMount.CheckMountAllowed( from ) )
			{
				// CheckMountAllowed sends the message
				return false;
			}
			else if( from.Mounted )
			{
				from.SendLocalizedMessage( 1005583 ); // Please dismount first.
				return false;
			}
			else if( from.IsBodyMod && !from.Body.IsHuman )
			{
				from.SendLocalizedMessage( 1061628 ); // You can't do that while polymorphed.
				return false;
			}
			else if( from.HasTrade )
			{
				from.SendLocalizedMessage( 1042317, "", 0x41 ); // You may not ride at this time
				return false;
			}
			else if( ( from.Followers + FollowerSlots ) > from.FollowersMax )
			{
				from.SendLocalizedMessage( 1049679 ); // You have too many followers to summon your mount.
				return false;
			}
			else if( !Multis.DesignContext.Check( from ) )
			{
				// Check sends the message
				return false;
			}

			return true;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( Validate( from ) )
				new EtherealSpell( this, from ).Cast();
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( m_IsDonationItem )
				LabelTo( from, "Donation Ethereal" );
			else
				LabelTo( from, "Veteran Reward" );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)3 ); // version

			writer.Write( m_IsDonationItem );
			writer.Write( m_IsRewardItem );

			writer.Write( (int)m_MountedID );
			writer.Write( (int)m_RegularID );
			writer.Write( m_Rider );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			LootType = LootType.Blessed;

			int version = reader.ReadInt();

			switch( version )
			{
				case 3:
				{
					m_IsDonationItem = reader.ReadBool();
					goto case 2;
				}
				case 2:
				{
					m_IsRewardItem = reader.ReadBool();
					goto case 0;
				}
				case 1: reader.ReadInt(); goto case 0;
				case 0:
				{
					m_MountedID = reader.ReadInt();
					m_RegularID = reader.ReadInt();
					m_Rider = reader.ReadMobile();

					if( m_MountedID == 0x3EA2 )
						m_MountedID = 0x3EAA;

					break;
				}
			}

			AddFollowers();

			if( version < 3 && Weight == 0 )
				Weight = -1;
		}

		public override DeathMoveResult OnParentDeath( Mobile parent )
		{
			Rider = null;//get off, move to pack

			return DeathMoveResult.RemainEquiped;
		}

		public static void Dismount( Mobile m )
		{
			IMount mount = m.Mount;

			if( mount != null )
				mount.Rider = null;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Rider
		{
			get
			{
				return m_Rider;
			}
			set
			{
				if( value != m_Rider )
				{
					if( value == null )
					{
						Internalize();
						UnmountMe();

						RemoveFollowers();
						m_Rider = value;
					}
					else
					{
						if( m_Rider != null )
							Dismount( m_Rider );

						Dismount( value );

						RemoveFollowers();
						m_Rider = value;
						AddFollowers();

						MountMe();
					}
				}
			}
		}

		public virtual int EtherealHue { get { return 0x4001; } }

		public void UnmountMe()
		{
			Container bp = m_Rider.Backpack;

			ItemID = m_RegularID;
			Layer = Layer.Invalid;
			Movable = true;

			if( Hue == EtherealHue )
				Hue = 0;

			if( bp != null )
			{
				bp.DropItem( this );
			}
			else
			{
				Point3D loc = m_Rider.Location;
				Map map = m_Rider.Map;

				if( map == null || map == Map.Internal )
				{
					loc = m_Rider.LogoutLocation;
					map = m_Rider.LogoutMap;
				}

				MoveToWorld( loc, map );
			}
		}

		public void MountMe()
		{
			ItemID = m_MountedID;
			Layer = Layer.Mount;
			Movable = false;

			if( Hue == 0 )
				Hue = EtherealHue;

			ProcessDelta();
			m_Rider.ProcessDelta();
			m_Rider.EquipItem( this );
			m_Rider.ProcessDelta();
			ProcessDelta();
		}

		public IMount Mount
		{
			get
			{
				return this;
			}
		}

		public static void StopMounting( Mobile mob )
		{
			if( mob.Spell is EtherealSpell )
				( (EtherealSpell)mob.Spell ).Stop();
		}

		public void OnRiderDamaged( int amount, Mobile from, bool willKill )
		{
		}

		private class EtherealSpell : Spell
		{
			private static SpellInfo m_Info = new SpellInfo( "Ethereal Mount", "", 230 );

			private EtherealMount m_Mount;
			private Mobile m_Rider;

			public EtherealSpell( EtherealMount mount, Mobile rider )
				: base( rider, null, m_Info )
			{
				m_Rider = rider;
				m_Mount = mount;
			}

			public override bool ClearHandsOnCast { get { return false; } }
			public override bool RevealOnCast { get { return false; } }

			public override TimeSpan GetCastRecovery()
			{
				return TimeSpan.Zero;
			}

			public override double CastDelayFastScalar { get { return 0; } }

			public override TimeSpan CastDelayBase
			{
				get
				{
					return TimeSpan.FromSeconds( ( ( m_Mount.IsDonationItem && RewardSystem.GetRewardLevel( m_Rider ) < 3 ) ? ( 7.5 + ( Core.AOS ? 3.0 : 2.0 ) ) : ( Core.AOS ? 3.0 : 2.0 ) ) );
				}
			}

			public override int GetMana()
			{
				return 0;
			}

			public override bool ConsumeReagents()
			{
				return true;
			}

			public override bool CheckFizzle()
			{
				return true;
			}

			private bool m_Stop;

			public void Stop()
			{
				m_Stop = true;
				Disturb( DisturbType.Hurt, false, false );
			}

			public override bool CheckDisturb( DisturbType type, bool checkFirst, bool resistable )
			{
				if( type == DisturbType.EquipRequest || type == DisturbType.UseRequest/* || type == DisturbType.Hurt*/ )
					return false;

				return true;
			}

			public override void DoHurtFizzle()
			{
				if( !m_Stop )
					base.DoHurtFizzle();
			}

			public override void DoFizzle()
			{
				if( !m_Stop )
					base.DoFizzle();
			}

			public override void OnDisturb( DisturbType type, bool message )
			{
				if( message && !m_Stop )
					Caster.SendLocalizedMessage( 1049455 ); // You have been disrupted while attempting to summon your ethereal mount!

				//m_Mount.UnmountMe();
			}

			public override void OnCast()
			{
				if( !m_Mount.Deleted && m_Mount.Rider == null && m_Mount.Validate( m_Rider ) )
					m_Mount.Rider = m_Rider;

				FinishSequence();
			}
		}
	}

	public class EtherealHorse : EtherealMount
	{
		public override int LabelNumber { get { return 1041298; } } // Ethereal Horse Statuette

		[Constructable]
		public EtherealHorse()
			: base( 0x20DD, 0x3EAA )
		{
		}

		public EtherealHorse( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal horse" )
				Name = null;

			if( ItemID == 0x2124 )
				ItemID = 0x20DD;
		}
	}

	public class EtherealLlama : EtherealMount
	{
		public override int LabelNumber { get { return 1041300; } } // Ethereal Llama Statuette

		[Constructable]
		public EtherealLlama()
			: base( 0x20F6, 0x3EAB )
		{
		}

		public EtherealLlama( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal llama" )
				Name = null;
		}
	}

	public class EtherealOstard : EtherealMount
	{
		public override int LabelNumber { get { return 1041299; } } // Ethereal Ostard Statuette

		[Constructable]
		public EtherealOstard()
			: base( 0x2135, 0x3EAC )
		{
		}

		public EtherealOstard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal ostard" )
				Name = null;
		}
	}

	public class EtherealRidgeback : EtherealMount
	{
		public override int LabelNumber { get { return 1049747; } } // Ethereal Ridgeback Statuette

		[Constructable]
		public EtherealRidgeback()
			: base( 0x2615, 0x3E9A )
		{
		}

		public EtherealRidgeback( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal ridgeback" )
				Name = null;
		}
	}

	public class EtherealUnicorn : EtherealMount
	{
		public override int LabelNumber { get { return 1049745; } } // Ethereal Unicorn Statuette

		[Constructable]
		public EtherealUnicorn()
			: base( 0x25CE, 0x3E9B )
		{
		}

		public EtherealUnicorn( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal unicorn" )
				Name = null;
		}
	}

	public class EtherealBeetle : EtherealMount
	{
		public override int LabelNumber { get { return 1049748; } } // Ethereal Beetle Statuette

		[Constructable]
		public EtherealBeetle()
			: base( 0x260F, 0x3E97 )
		{
		}

		public EtherealBeetle( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal beetle" )
				Name = null;
		}
	}

	public class EtherealKirin : EtherealMount
	{
		public override int LabelNumber { get { return 1049746; } } // Ethereal Ki-Rin Statuette

		[Constructable]
		public EtherealKirin()
			: base( 0x25A0, 0x3E9C )
		{
		}

		public EtherealKirin( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal kirin" )
				Name = null;
		}
	}

	public class EtherealSwampDragon : EtherealMount
	{
		public override int LabelNumber { get { return 1049749; } } // Ethereal Swamp Dragon Statuette

		[Constructable]
		public EtherealSwampDragon()
			: base( 0x2619, 0x3E98 )
		{
		}

		public EtherealSwampDragon( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( Name == "an ethereal swamp dragon" )
				Name = null;
		}
	}

	public class RideablePolarBear : EtherealMount
	{
		public override int LabelNumber { get { return 1076159; } } // Rideable Polar Bear 
		public override int EtherealHue { get { return 0; } }

		[Constructable]
		public RideablePolarBear()
			: base( 0x20E1, 0x3EC5 )
		{
		}

		public RideablePolarBear( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class EtherealCuSidhe : EtherealMount
	{
		public override int LabelNumber { get { return 1080386; } } // Ethereal Cu Sidhe Statuette

		[Constructable]
		public EtherealCuSidhe()
			: base( 0x2D96, 0x3E91 )
		{
		}

		public EtherealCuSidhe( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class EtherealHiryu : EtherealMount
	{
		public override int LabelNumber { get { return 1113813; } } // Ethereal Hiryu Statuette

		[Constructable]
		public EtherealHiryu()
			: base( 0x276A, 0x3E94 )
		{
		}

		public EtherealHiryu( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class EtherealReptalon : EtherealMount
	{
		public override int LabelNumber { get { return 1113812; } } // Ethereal Reptalon Statuette

		[Constructable]
		public EtherealReptalon()
			: base( 0x2d95, 0x3e90 )
		{
		}

		public EtherealReptalon( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class ChargerOfTheFallen : EtherealMount
	{
		public override int LabelNumber { get { return 1074816; } } // Charger of the Fallen Statuette

		[Constructable]
		public ChargerOfTheFallen()
			: base( 0x2D9C, 0x3E92 )
		{
		}

		public override int EtherealHue { get { return 0; } }

		public ChargerOfTheFallen( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if( version <= 1 && Hue != 0 )
			{
				Hue = 0;
			}
		}
	}
}
