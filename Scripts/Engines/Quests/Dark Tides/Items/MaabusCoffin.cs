using System;
using Server;
using Server.Items;

namespace Server.Engines.Quests.Necro
{
	public class MaabusCoffin : BaseAddon
	{
		private Maabus m_Maabus;
		private Point3D m_SpawnLocation;

		[CommandProperty( AccessLevel.GameMaster )]
		public Maabus Maabus { get { return m_Maabus; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D SpawnLocation { get { return m_SpawnLocation; } set { m_SpawnLocation = value; } }

		[Constructable]
		public MaabusCoffin()
		{
			AddComponent( new MaabusCoffinComponent( 0x1C2B, 0x1C2B ), -1, -1, 0 );

			AddComponent( new MaabusCoffinComponent( 0x1D16, 0x1C2C ),  0, -1, 0 );
			AddComponent( new MaabusCoffinComponent( 0x1D17, 0x1C2D ),  1, -1, 0 );
			AddComponent( new MaabusCoffinComponent( 0x1D51, 0x1C2E ),  2, -1, 0 );

			AddComponent( new MaabusCoffinComponent( 0x1D4E, 0x1C2A ),  0,  0, 0 );
			AddComponent( new MaabusCoffinComponent( 0x1D4D, 0x1C29 ),  1,  0, 0 );
			AddComponent( new MaabusCoffinComponent( 0x1D4C, 0x1C28 ),  2,  0, 0 );
		}

		public void Awake( Mobile caller )
		{
			if ( m_Maabus != null || m_SpawnLocation == Point3D.Zero )
				return;

			foreach ( MaabusCoffinComponent c in Components )
				c.TurnToEmpty();

			m_Maabus = new Maabus();

			m_Maabus.Location = m_SpawnLocation;
			m_Maabus.Map = Map;

			m_Maabus.Direction = m_Maabus.GetDirectionTo( caller );

			Timer.DelayCall( TimeSpan.FromSeconds( 7.5 ), new TimerCallback( BeginSleep ) );
		}

		public void BeginSleep()
		{
			if ( m_Maabus == null )
				return;

			Effects.PlaySound( m_Maabus.Location, m_Maabus.Map, 0x48E );

			Timer.DelayCall( TimeSpan.FromSeconds( 2.5 ), new TimerCallback( Sleep ) );
		}

		public void Sleep()
		{
			if ( m_Maabus == null )
				return;

			Effects.SendLocationParticles( EffectItem.Create( m_Maabus.Location, m_Maabus.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 0x7E7 );
			Effects.PlaySound( m_Maabus.Location, m_Maabus.Map, 0x1FE );

			m_Maabus.Delete();
			m_Maabus = null;

			foreach ( MaabusCoffinComponent c in Components )
				c.TurnToFull();
		}

		public MaabusCoffin( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) m_Maabus );
			writer.Write( (Point3D) m_SpawnLocation );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Maabus = reader.ReadMobile() as Maabus;
			m_SpawnLocation = reader.ReadPoint3D();

			Sleep();
		}
	}

	public class MaabusCoffinComponent : AddonComponent
	{
		private int m_FullItemID;
		private int m_EmptyItemID;

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D SpawnLocation
		{
			get { return Addon is MaabusCoffin ? ((MaabusCoffin)Addon).SpawnLocation : Point3D.Zero; }
			set { if ( Addon is MaabusCoffin ) ((MaabusCoffin)Addon).SpawnLocation = value; }
		}

		public MaabusCoffinComponent( int itemID ) : this( itemID, itemID )
		{
		}

		public MaabusCoffinComponent( int fullItemID, int emptyItemID ) : base( fullItemID )
		{
			m_FullItemID = fullItemID;
			m_EmptyItemID = emptyItemID;
		}

		public void TurnToEmpty()
		{
			ItemID = m_EmptyItemID;
		}

		public void TurnToFull()
		{
			ItemID = m_FullItemID;
		}

		public MaabusCoffinComponent( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_FullItemID );
			writer.Write( (int) m_EmptyItemID );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_FullItemID = reader.ReadInt();
			m_EmptyItemID = reader.ReadInt();
		}
	}
}