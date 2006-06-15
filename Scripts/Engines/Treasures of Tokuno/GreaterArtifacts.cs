using Server;
using System;
using Server.Misc;
using Server.Mobiles;

namespace Server.Items
{
	public class DarkenedSky : Kama
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber { get { return 1070966; } } // Darkened Sky

		[Constructable]
		public DarkenedSky() : base()
		{
			WeaponAttributes.HitLightning = 60;
			Attributes.WeaponSpeed = 25;
			Attributes.WeaponDamage = 50;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = pois = 0;
			cold = nrgy = 50;
		}

		public DarkenedSky( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

	}

	public class KasaOfTheRajin : Kasa
	{
		public override int LabelNumber { get { return 1070969; } } // Kasa of the Raj-in

		public override int BasePhysicalResistance { get { return 12; } }
		public override int BaseFireResistance { get { return 17; } }
		public override int BaseColdResistance { get { return 21; } }
		public override int BasePoisonResistance { get { return 17; } }
		public override int BaseEnergyResistance { get { return 17; } }

		public override bool CanBeBlessed { get { return false; } }

		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		[Constructable]
		public KasaOfTheRajin() : base()
		{
			Attributes.SpellDamage = 12;
		}

		public KasaOfTheRajin( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)2 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version <= 1 )
			{
				MaxHitPoints = 255;
				HitPoints = 255;
			}

			if( version == 0 )
				LootType = LootType.Regular;
		}

	}

	public class RuneBeetleCarapace : PlateDo
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber{ get{ return 1070968; } } // Rune Beetle Carapace

		public override int BaseColdResistance { get { return 14; } }
		public override int BaseEnergyResistance { get { return 14; } }

		[Constructable]
		public RuneBeetleCarapace() : base()
		{
			Attributes.BonusMana = 10;
			Attributes.RegenMana = 3;
			Attributes.LowerManaCost = 15;
			ArmorAttributes.LowerStatReq = 100;
			ArmorAttributes.MageArmor = 1;
		}

		public RuneBeetleCarapace( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

	}

	public class Stormgrip : LeatherGloves
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber{ get{ return 1070970; } } // Stormgrip

		public override int BasePhysicalResistance { get { return 10; } }
		public override int BaseColdResistance { get { return 18; } }
		public override int BaseEnergyResistance { get { return 18; } }

		[Constructable]
		public Stormgrip() : base()
		{
			Attributes.BonusInt = 8;
			Attributes.Luck = 125;
			Attributes.WeaponDamage = 25;
		}

		public Stormgrip( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

	}

	public class SwordOfTheStampede : NoDachi
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber { get { return 1070964; } } // Sword of the Stampede

		[Constructable]
		public SwordOfTheStampede() : base()
		{
			WeaponAttributes.HitHarm = 100;
			Attributes.AttackChance = 10;
			Attributes.WeaponDamage = 60;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = pois = nrgy = 0;
			cold = 100;
		}

		public SwordOfTheStampede( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

	}

	public class SwordsOfProsperity : Daisho
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber { get { return 1070963; } } // Swords of Prosperity

		[Constructable]
		public SwordsOfProsperity() : base()
		{
			WeaponAttributes.MageWeapon = 30;
			Attributes.SpellChanneling = 1;
			Attributes.CastSpeed = 1;
			Attributes.Luck = 200;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = cold = pois = nrgy = 0;
			fire = 100;
		}

		public SwordsOfProsperity( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

	}

	public class TheHorselord : Yumi
	{
		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		public override int LabelNumber { get { return 1070967; } } // The Horselord

		[Constructable]
		public TheHorselord() : base()
		{
			Attributes.BonusDex = 5;
			Attributes.RegenMana = 1;
			Attributes.Luck = 125;
			Attributes.WeaponDamage = 50;

			Slayer = SlayerName.ElementalBan;
			Slayer2 = SlayerName.ReptilianDeath;
		}

		public TheHorselord( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class TomeOfLostKnowledge : Spellbook
	{
		public override int LabelNumber { get { return 1070971; } } // Tome of Lost Knowledge

		[Constructable]
		public TomeOfLostKnowledge() : base()
		{
			LootType = LootType.Regular;
			Hue = 0x530;

			SkillBonuses.SetValues( 0, SkillName.Magery, 15.0 );
			Attributes.BonusInt = 8;
			Attributes.LowerManaCost = 15;
			Attributes.SpellDamage = 15;
		}

		public TomeOfLostKnowledge( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class WindsEdge : Tessen
	{
		public override int LabelNumber { get { return 1070965; } } // Wind's Edge

		[Constructable]
		public WindsEdge() : base()
		{
			WeaponAttributes.HitLeechMana = 40;

			Attributes.WeaponDamage = 50;
			Attributes.WeaponSpeed = 50;
			Attributes.DefendChance = 10;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = cold = pois = 0;
			nrgy = 100;
		}


		public WindsEdge( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }
	}

	public enum PigmentType
	{
		None,
		ParagonGold,
		VioletCouragePurple,
		InvulnerabilityBlue,
		LunaWhite,
		DryadGreen,
		ShadowDancerBlack,
		BerserkerRed,
		NoxGreen,
		RumRed,
		FireOrange
	}

	public class PigmentsOfTokuno : Item, IUsesRemaining
	{
		public class PigmentInfo
		{
			//private PigmentType m_PigmentType;
			private int m_Hue;
			private TextDefinition m_Label;

			public int Hue { get { return m_Hue; } }
			public TextDefinition Label { get { return m_Label; } }

			public PigmentInfo( int hue, TextDefinition label )
			{
				m_Hue = hue;
				m_Label = label;
			}

			private static PigmentInfo[] m_Table = new PigmentInfo[]
				{
					new PigmentInfo( /*PigmentType.None,*/ 0, -1 ),
					new PigmentInfo( /*PigmentType.ParagonGold,*/ 0x501, 1070987 ),
					new PigmentInfo( /*PigmentType.VioletCouragePurple,*/ 0x486, 1070988 ),
					new PigmentInfo( /*PigmentType.InvulnerabilityBlue,*/ 0x4F2, 1070989 ),
					new PigmentInfo( /*PigmentType.LunaWhite,*/ 0x47E, 1070990 ),
					new PigmentInfo( /*PigmentType.DryadGreen,*/ 0x48F, 1070991 ),
					new PigmentInfo( /*PigmentType.ShadowDancerBlack,*/ 0x455, 1070992 ),
					new PigmentInfo( /*PigmentType.BerserkerRed,*/ 0x21, 1070993 ),
					new PigmentInfo( /*PigmentType.NoxGreen,*/ 0x58C, 1070994 ),
					new PigmentInfo( /*PigmentType.RumRed,*/ 0x66C, 1070995 ),
					new PigmentInfo( /*PigmentType.FireOrange,*/ 0x54F, 1070996 )
				};

			public static PigmentInfo GetInfo( PigmentType type )
			{
				int v = (int)type;

				if( v < 0 || v >= m_Table.Length )
					v = 0;

				return m_Table[v];
			}
		}

		private PigmentType m_Type;

		[CommandProperty( AccessLevel.GameMaster )]
		public PigmentType Type
		{
			get { return m_Type; }
			set
			{
				m_Type = value;

				PigmentInfo p = PigmentInfo.GetInfo( m_Type );
				Hue = p.Hue;
				InvalidateProperties();
			}
		}

		private int m_UsesRemaining;

		public override int LabelNumber { get { return 1070933; } } // Pigments of Tokuno

		[Constructable]
		public PigmentsOfTokuno() : this( PigmentType.None, 10 )
		{
		}

		[Constructable]
		public PigmentsOfTokuno( PigmentType type ) : this( type, (type == PigmentType.None)? 10 : 50 )
		{
		}

		[Constructable]
		public PigmentsOfTokuno( PigmentType type, int uses ) : base( 0xEFF )
		{
			Weight = 1.0;
			m_UsesRemaining = uses;
			Type = type;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if( m_Type != PigmentType.None )
			{
				PigmentInfo p = PigmentInfo.GetInfo( m_Type );
				TextDefinition.AddTo( list, p.Label );
			}

			list.Add( 1060584, m_UsesRemaining.ToString() ); // uses remaining: ~1_val~
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( IsAccessibleTo( from ) && from.InRange( GetWorldLocation(), 3 ) )
			{
				from.SendLocalizedMessage( 1070929 ); // Select the artifact or enhanced magic item to dye.
				from.BeginTarget( 3, false, Server.Targeting.TargetFlags.None, new TargetStateCallback( InternalCallback ), this );
			}
			else
				from.SendLocalizedMessage( 502436 ); // That is not accessible.
		}

		private void InternalCallback( Mobile from, object targeted, object state )
		{
			PigmentsOfTokuno pigment = (PigmentsOfTokuno)state;

			if( pigment.Deleted || pigment.UsesRemaining <= 0 || !from.InRange( pigment.GetWorldLocation(), 3 ) || !pigment.IsAccessibleTo( from ))
				return;

			Item i = targeted as Item;

			if( i == null )
				from.SendLocalizedMessage( 1070931 ); // You can only dye artifacts and enhanced magic items with this tub.
			else if( !from.InRange( i.GetWorldLocation(), 3 ) || !IsAccessibleTo( from ) )
				from.SendLocalizedMessage( 502436 ); // That is not accessible.
			else if( from.Items.Contains( i ) )
				from.SendLocalizedMessage( 1070930 ); // Can't dye artifacts or enhanced magic items that are being worn.
			else if( i.IsLockedDown )
				from.SendLocalizedMessage( 1070932 ); // You may not dye artifacts and enhanced magic items which are locked down.
			else if( i is PigmentsOfTokuno )
				from.SendLocalizedMessage( 1042417 ); // You cannot dye that.
			else if( !IsValidItem( i ) )
				from.SendLocalizedMessage( 1070931 ); // You can only dye artifacts and enhanced magic items with this tub.	//Yes, it says tub on OSI.  Don't ask me why ;p
			else
			{
				//Notes: on OSI there IS no hue check to see if it's already hued.  and no messages on successful hue either
				i.Hue = PigmentInfo.GetInfo( pigment.Type ).Hue;

				if( --pigment.UsesRemaining <= 0 )
					pigment.Delete();
			}
		}

		public static bool IsValidItem( Item i )
		{
			if( i is PigmentsOfTokuno )
				return false;

			Type t = i.GetType();

			CraftResource resource = CraftResource.None;

			if( i is BaseWeapon )
				resource = ((BaseWeapon)i).Resource;
			else if( i is BaseArmor )
				resource = ((BaseArmor)i).Resource;

			if( !CraftResources.IsStandard( resource ) )
				return true;

			return( 
				IsInTypeList( t, TreasuresOfTokuno.LesserArtifacts )
				|| IsInTypeList( t, TreasuresOfTokuno.GreaterArtifacts ) 
				|| IsInTypeList( t, DemonKnight.ArtifactRarity10 )
				|| IsInTypeList( t, DemonKnight.ArtifactRarity11 )
				|| IsInTypeList( t, DemonKnight.ArtifactRarity10 )
				|| IsInTypeList( t, StealableArtifactsSpawner.TypesOfEntires )
				|| IsInTypeList( t, Paragon.Artifacts )
				|| IsInTypeList( t, Leviathan.Artifacts )
				|| IsInTypeList( t, TreasureMapChest.Artifacts )
				);
		}

		private static bool IsInTypeList( Type t, Type[] list )
		{
			for( int i = 0; i < list.Length; i++ )
			{
				if( list[i] == t ) return true;
			}

			return false;
		}

		public PigmentsOfTokuno( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );

			writer.WriteEncodedInt( (int)m_Type );
			writer.WriteEncodedInt( m_UsesRemaining );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Type = (PigmentType)reader.ReadEncodedInt();
			m_UsesRemaining = reader.ReadEncodedInt();
		}

		#region IUsesRemaining Members

		[CommandProperty( AccessLevel.GameMaster )]
		public int UsesRemaining
		{
			get { return m_UsesRemaining; }
			set { m_UsesRemaining = value; InvalidateProperties(); }
		}

		public bool ShowUsesRemaining
		{
			get { return true; }
			set {}
		}


		#endregion
	}
}