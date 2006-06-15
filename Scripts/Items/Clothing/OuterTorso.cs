using System;

namespace Server.Items
{
	public abstract class BaseOuterTorso : BaseClothing
	{
		public BaseOuterTorso( int itemID ) : this( itemID, 0 )
		{
		}

		public BaseOuterTorso( int itemID, int hue ) : base( itemID, Layer.OuterTorso, hue )
		{
		}

		public BaseOuterTorso( Serial serial ) : base( serial )
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

	[Flipable( 0x230E, 0x230D )]
	public class GildedDress : BaseOuterTorso
	{
		[Constructable]
		public GildedDress() : this( 0 )
		{
		}

		[Constructable]
		public GildedDress( int hue ) : base( 0x230E, hue )
		{
			Weight = 3.0;
		}

		public GildedDress( Serial serial ) : base( serial )
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

	[Flipable( 0x1F00, 0x1EFF )]
	public class FancyDress : BaseOuterTorso
	{
		[Constructable]
		public FancyDress() : this( 0 )
		{
		}

		[Constructable]
		public FancyDress( int hue ) : base( 0x1F00, hue )
		{
			Weight = 3.0;
		}

		public FancyDress( Serial serial ) : base( serial )
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

	public class DeathRobe : Robe
	{
		public override bool DisplayLootType
		{
			get{ return false; }
		}

		[Constructable]
		public DeathRobe()
		{
			LootType = LootType.Newbied;
			Hue = 2301;
		}

		public new bool Scissor( Mobile from, Scissors scissors )
		{
			from.SendLocalizedMessage( 502440 ); // Scissors can not be used on that to produce anything.
			return false;
		}

		public DeathRobe( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version < 1 && Hue == 0 )
				Hue = 2301;
		}
	}

	[Flipable]
	public class RewardRobe : BaseOuterTorso, Engines.VeteranRewards.IRewardItem
	{
		private int m_LabelNumber;
		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Number
		{
			get{ return m_LabelNumber; }
			set{ m_LabelNumber = value; InvalidateProperties(); }
		}

		public override int LabelNumber
		{
			get
			{
				if ( m_LabelNumber > 0 )
					return m_LabelNumber;

				return base.LabelNumber;
			}
		}

		public override int BasePhysicalResistance{ get{ return 3; } }

		public override void OnAdded( object parent )
		{
			base.OnAdded( parent );

			if ( parent is Mobile )
				((Mobile)parent).VirtualArmorMod += 2;
		}

		public override void OnRemoved(object parent)
		{
			base.OnRemoved( parent );

			if ( parent is Mobile )
				((Mobile)parent).VirtualArmorMod -= 2;
		}

		public override bool Dye( Mobile from, DyeTub sender )
		{
			from.SendLocalizedMessage( sender.FailMessage );
			return false;
		}

		public override bool CanEquip( Mobile m )
		{
			if ( !base.CanEquip( m ) )
				return false;

			return !m_IsRewardItem || Engines.VeteranRewards.RewardSystem.CheckIsUsableBy( m, this, new object[]{ Hue, m_LabelNumber } );
		}

		[Constructable]
		public RewardRobe() : this( 0 )
		{
		}

		[Constructable]
		public RewardRobe( int hue ) : this( hue, 0 )
		{
		}

		[Constructable]
		public RewardRobe( int hue, int labelNumber ) : base( 0x1F03, hue )
		{
			Weight = 3.0;
			LootType = LootType.Blessed;

			m_LabelNumber = labelNumber;
		}

		public RewardRobe( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_LabelNumber );
			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_LabelNumber = reader.ReadInt();
					m_IsRewardItem = reader.ReadBool();
					break;
				}
			}

			if ( Parent is Mobile )
				((Mobile)Parent).VirtualArmorMod += 2;
		}
	}

	[Flipable]
	public class Robe : BaseOuterTorso, IArcaneEquip
	{
		#region Arcane Impl
		private int m_MaxArcaneCharges, m_CurArcaneCharges;

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxArcaneCharges
		{
			get{ return m_MaxArcaneCharges; }
			set{ m_MaxArcaneCharges = value; InvalidateProperties(); Update(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int CurArcaneCharges
		{
			get{ return m_CurArcaneCharges; }
			set{ m_CurArcaneCharges = value; InvalidateProperties(); Update(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsArcane
		{
			get{ return ( m_MaxArcaneCharges > 0 && m_CurArcaneCharges >= 0 ); }
		}

		public void Update()
		{
			if ( IsArcane )
				ItemID = 0x26AE;
			else if ( ItemID == 0x26AE )
				ItemID = 0x1F04;

			if ( IsArcane && CurArcaneCharges == 0 )
				Hue = 0;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( IsArcane )
				list.Add( 1061837, "{0}\t{1}", m_CurArcaneCharges, m_MaxArcaneCharges ); // arcane charges: ~1_val~ / ~2_val~
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( IsArcane )
				LabelTo( from, 1061837, String.Format( "{0}\t{1}", m_CurArcaneCharges, m_MaxArcaneCharges ) );
		}

		public void Flip()
		{
			if ( ItemID == 0x1F03 )
				ItemID = 0x1F04;
			else if ( ItemID == 0x1F04 )
				ItemID = 0x1F03;
		}
		#endregion

		[Constructable]
		public Robe() : this( 0 )
		{
		}

		[Constructable]
		public Robe( int hue ) : base( 0x1F03, hue )
		{
			Weight = 3.0;
		}

		public Robe( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			if ( IsArcane )
			{
				writer.Write( true );
				writer.Write( (int) m_CurArcaneCharges );
				writer.Write( (int) m_MaxArcaneCharges );
			}
			else
			{
				writer.Write( false );
			}
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					if ( reader.ReadBool() )
					{
						m_CurArcaneCharges = reader.ReadInt();
						m_MaxArcaneCharges = reader.ReadInt();

						if ( Hue == 2118 )
							Hue = ArcaneGem.DefaultArcaneHue;
					}

					break;
				}
			}
		}
	}

	[Flipable( 0x2684, 0x2683 )]
	public class HoodedShroudOfShadows : BaseOuterTorso
	{
		[Constructable]
		public HoodedShroudOfShadows() : this( 0x455 )
		{
		}

		[Constructable]
		public HoodedShroudOfShadows( int hue ) : base( 0x2684, hue )
		{
			LootType = LootType.Blessed;
			Weight = 3.0;
		}

		public override bool Dye( Mobile from, DyeTub sender )
		{
			from.SendLocalizedMessage( sender.FailMessage );
			return false;
		}

		public HoodedShroudOfShadows( Serial serial ) : base( serial )
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

	[Flipable( 0x1f01, 0x1f02 )]
	public class PlainDress : BaseOuterTorso
	{
		[Constructable]
		public PlainDress() : this( 0 )
		{
		}

		[Constructable]
		public PlainDress( int hue ) : base( 0x1F01, hue )
		{
			Weight = 2.0;
		}

		public PlainDress( Serial serial ) : base( serial )
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

			if ( Weight == 3.0 )
				Weight = 2.0;
		}
	}

	[Flipable( 0x2799, 0x27E4 )]
	public class Kamishimo : BaseOuterTorso
	{
		[Constructable]
		public Kamishimo() : this( 0 )
		{
		}

		[Constructable]
		public Kamishimo( int hue ) : base( 0x2799, hue )
		{
			Weight = 3.0;
		}

		public Kamishimo( Serial serial ) : base( serial )
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

	[Flipable( 0x279C, 0x27E7 )]
	public class HakamaShita : BaseOuterTorso
	{
		[Constructable]
		public HakamaShita() : this( 0 )
		{
		}

		[Constructable]
		public HakamaShita( int hue ) : base( 0x279C, hue )
		{
			Weight = 3.0;
		}

		public HakamaShita( Serial serial ) : base( serial )
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

	[Flipable( 0x2782, 0x27CD )]
	public class MaleKimono : BaseOuterTorso
	{
		[Constructable]
		public MaleKimono() : this( 0 )
		{
		}

		[Constructable]
		public MaleKimono( int hue ) : base( 0x2782, hue )
		{
			Weight = 3.0;
		}

		public override bool AllowFemaleWearer{ get{ return false; } }

		public MaleKimono( Serial serial ) : base( serial )
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

	[Flipable( 0x2783, 0x27CE )]
	public class FemaleKimono : BaseOuterTorso
	{
		[Constructable]
		public FemaleKimono() : this( 0 )
		{
		}

		[Constructable]
		public FemaleKimono( int hue ) : base( 0x2783, hue )
		{
			Weight = 3.0;
		}

		public override bool AllowMaleWearer{ get{ return false; } }

		public FemaleKimono( Serial serial ) : base( serial )
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

	[Flipable( 0x2FB9, 0x3173 )]
	public class MaleElvenRobe : BaseOuterTorso
	{
		// TODO: Supports arcane?
		public override Race RequiredRace { get { return Race.Elf; } }

		[Constructable]
		public MaleElvenRobe() : this( 0 )
		{
		}

		[Constructable]
		public MaleElvenRobe( int hue ) : base( 0x2FB9, hue )
		{
			Weight = 2.0;
		}

		public override bool AllowFemaleWearer{ get{ return false; } }

		public MaleElvenRobe( Serial serial ) : base( serial )
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

	[Flipable( 0x2FBA, 0x3174 )]
	public class FemaleElvenRobe : BaseOuterTorso
	{
		// TODO: Supports arcane?
		public override Race RequiredRace { get { return Race.Elf; } }
		[Constructable]
		public FemaleElvenRobe() : this( 0 )
		{
		}

		[Constructable]
		public FemaleElvenRobe( int hue ) : base( 0x2FBA, hue )
		{
			Weight = 2.0;
		}

		public override bool AllowMaleWearer{ get{ return false; } }

		public FemaleElvenRobe( Serial serial ) : base( serial )
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
}