using System;
using Server;
using Server.Targeting;

namespace Server.Engines.Plants
{
	public class Seed : Item
	{
		private PlantType m_PlantType;
		private PlantHue m_PlantHue;
		private bool m_ShowType;

		[CommandProperty( AccessLevel.GameMaster )]
		public PlantType PlantType
		{
			get { return m_PlantType; }
			set
			{
				m_PlantType = value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public PlantHue PlantHue
		{
			get { return m_PlantHue; }
			set
			{
				m_PlantHue = value;
				Hue = PlantHueInfo.GetInfo( value ).Hue;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool ShowType
		{
			get { return m_ShowType; }
			set
			{
				m_ShowType = value;
				InvalidateProperties();
			}
		}

		public override int LabelNumber{ get { return 1060810; } } // seed

		public static Seed RandomBonsaiSeed()
		{
			return RandomBonsaiSeed( 0.5 );
		}

		public static Seed RandomBonsaiSeed( double increaseRatio )
		{
			return new Seed( PlantTypeInfo.RandomBonsai( increaseRatio ), PlantHue.Plain, false );
		}

		[Constructable]
		public Seed() : this( PlantTypeInfo.RandomFirstGeneration(), PlantHueInfo.RandomFirstGeneration(), false )
		{
		}

		[Constructable]
		public Seed( PlantType plantType, PlantHue plantHue, bool showType ) : base( 0xDCF )
		{
			Weight = 1.0;

			m_PlantType = plantType;
			m_PlantHue = plantHue;
			m_ShowType = showType;

			Hue = PlantHueInfo.GetInfo( plantHue ).Hue;
		}

		public Seed( Serial serial ) : base( serial )
		{
		}

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } }

		public override void AddNameProperty( ObjectPropertyList list )
		{
			PlantHueInfo hueInfo = PlantHueInfo.GetInfo( m_PlantHue );

			int title = PlantTypeInfo.GetBonsaiTitle( m_PlantType );
			if ( title == 0 ) // Not a bonsai
				title = hueInfo.Name;

			if ( m_ShowType )
			{
				PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo( m_PlantType );
				list.Add( hueInfo.IsBright() ? 1061918 : 1061917, String.Concat( "#", title.ToString(), "\t#", typeInfo.Name.ToString() ) ); // [bright] ~1_COLOR~ ~2_TYPE~ seed
			}
			else
			{
				list.Add( hueInfo.IsBright() ? 1060839 : 1060838, String.Concat( "#", title.ToString() ) ); // [bright] ~1_val~ seed
			}
		}

		public override void OnSingleClick ( Mobile from )
		{
			PlantHueInfo hueInfo = PlantHueInfo.GetInfo( m_PlantHue );

			if ( m_ShowType )
			{
				PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo( m_PlantType );
				LabelTo( from, hueInfo.IsBright() ? 1061918 : 1061917, String.Concat( "#", hueInfo.Name.ToString(), "\t#", typeInfo.Name.ToString() ) ); // [bright] ~1_COLOR~ ~2_TYPE~ seed
			}
			else
			{
				LabelTo( from, hueInfo.IsBright() ? 1060839 : 1060838, String.Concat( "#", hueInfo.Name.ToString() ) ); // [bright] ~1_val~ seed
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1042664 ); // You must have the object in your backpack to use it.
				return;
			}

			from.Target = new InternalTarget( this );
			LabelTo( from, 1061916 ); // Choose a bowl of dirt to plant this seed in.
		}

		private class InternalTarget : Target
		{
			private Seed m_Seed;

			public InternalTarget( Seed seed ) : base( 3, false, TargetFlags.None )
			{
				m_Seed = seed;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Seed.Deleted )
					return;

				if ( !m_Seed.IsChildOf( from.Backpack ) )
				{
					from.SendLocalizedMessage( 1042664 ); // You must have the object in your backpack to use it.
					return;
				}

				if ( targeted is PlantItem )
				{
					PlantItem plant = (PlantItem)targeted;

					plant.PlantSeed( from, m_Seed );
				}
				else if ( targeted is Item )
				{
					((Item)targeted).LabelTo( from, 1061919 ); // You must use a seed on a bowl of dirt!
				}
				else
				{
					from.SendLocalizedMessage( 1061919 ); // You must use a seed on a bowl of dirt!
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_PlantType );
			writer.Write( (int) m_PlantHue );
			writer.Write( (bool) m_ShowType );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_PlantType = (PlantType)reader.ReadInt();
			m_PlantHue = (PlantHue)reader.ReadInt();
			m_ShowType = reader.ReadBool();

			if ( Weight != 1.0 )
				Weight = 1.0;
		}
	}
}