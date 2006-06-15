using Server;
using System;

namespace Server.Items
{
	public class AncientFarmersKasa : Kasa
	{
		public override int LabelNumber{ get{ return 1070922; } } // Ancient Farmer's Kasa
		public override int BaseColdResistance { get { return 19; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get { return 255; } }

		[Constructable]
		public AncientFarmersKasa() : base()
		{
			Attributes.BonusStr = 5;
			Attributes.BonusStam = 5;
			Attributes.RegenStam = 5;

			SkillBonuses.SetValues( 0, SkillName.AnimalLore, 5.0 );
		}

		public AncientFarmersKasa( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version <= 1 )
			{
				MaxHitPoints = 255;
				HitPoints = 255;
			}

			if( version == 0 )
				SkillBonuses.SetValues( 0, SkillName.AnimalLore, 5.0 );
		}
	}

	public class AncientSamuraiDo : PlateDo 
	{
		public override int LabelNumber { get { return 1070926; } } // Ancient Samurai Do

		public override int BasePhysicalResistance { get { return 15; } }
		public override int BaseFireResistance { get { return 12; } }
		public override int BaseColdResistance { get { return 10; } }
		public override int BasePoisonResistance { get { return 11; } }
		public override int BaseEnergyResistance { get { return 8; } }

		[Constructable]
		public AncientSamuraiDo() : base()
		{
			ArmorAttributes.LowerStatReq = 100;
			ArmorAttributes.MageArmor = 1;
			SkillBonuses.SetValues( 0, SkillName.Parry, 10.0 );
		}

		public AncientSamuraiDo( Serial serial ) : base( serial )
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

	public class ArmsOfTacticalExcellence : LeatherHiroSode
	{
		public override int LabelNumber { get { return 1070921; } } // Arms of Tactical Excellence

		public override int BaseFireResistance { get { return 9; } }
		public override int BaseColdResistance { get { return 13; } }
		public override int BasePoisonResistance { get { return 8; } }

		[Constructable]
		public ArmsOfTacticalExcellence() : base()
		{
			Attributes.BonusDex = 5;
			SkillBonuses.SetValues( 0, SkillName.Tactics, 12.0 );
		}

		public ArmsOfTacticalExcellence( Serial serial ) : base( serial )
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

	public class BlackLotusHood : ClothNinjaHood
	{
		public override int LabelNumber { get { return 1070919; } } // Black Lotus Hood

		public override int BasePhysicalResistance { get { return 0; } }
		public override int BaseFireResistance { get { return 11; } }
		public override int BaseColdResistance { get { return 15; } }
		public override int BasePoisonResistance { get { return 11; } }
		public override int BaseEnergyResistance { get { return 11; } }

		public override int InitMinHits { get { return 255; } }
		public override int InitMaxHits { get { return 255; } }

		[Constructable]
		public BlackLotusHood() : base()
		{
			Attributes.LowerManaCost = 6;
			Attributes.AttackChance = 6;
			ClothingAttributes.SelfRepair = 5;
		}

		public BlackLotusHood( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)1 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version == 0 )
			{
				MaxHitPoints = 255;
				HitPoints = 255;
			}
		}
	}

	public class DaimyosHelm : PlateBattleKabuto
	{
		public override int LabelNumber { get { return 1070920; } } // Daimyo's Helm

		public override int BaseColdResistance { get { return 10; } }

		[Constructable]
		public DaimyosHelm() : base()
		{
			ArmorAttributes.LowerStatReq = 100;
			ArmorAttributes.MageArmor = 1;
			ArmorAttributes.SelfRepair = 3;
			Attributes.WeaponSpeed = 10;
		}

		public DaimyosHelm( Serial serial ) : base( serial )
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

	public class DemonForks : Sai
	{
		public override int LabelNumber{ get{ return 1070917; } } // Demon Forks

		[Constructable]
		public DemonForks() : base()
		{
			WeaponAttributes.ResistFireBonus = 10;
			WeaponAttributes.ResistPoisonBonus = 10;

			Attributes.ReflectPhysical = 10;
			Attributes.WeaponDamage = 35;
			Attributes.DefendChance = 10;
		}

		public DemonForks( Serial serial ) : base( serial )
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

	public class DragonNunchaku : Nunchaku
	{
		public override int LabelNumber{ get{ return 1070914; } } // Dragon Nunchaku

		[Constructable]
		public DragonNunchaku() : base()
		{
			WeaponAttributes.ResistFireBonus = 5;
			WeaponAttributes.SelfRepair = 3;
			WeaponAttributes.HitFireball = 50;

			Attributes.WeaponDamage = 40;
			Attributes.WeaponSpeed = 20;
		}

		public DragonNunchaku( Serial serial ) : base( serial )
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

	public class Exiler : Tetsubo
	{
		public override int LabelNumber{ get{ return 1070913; } } // Exiler

		[Constructable]
		public Exiler() : base()
		{
			WeaponAttributes.HitDispel = 33;
			Slayer = SlayerName.Exorcism;

			Attributes.WeaponDamage = 40;
			Attributes.WeaponSpeed = 20;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = cold = pois = 0;

			nrgy = 100;
		}


		public Exiler( Serial serial ) : base( serial )
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

	public class GlovesOfTheSun : LeatherNinjaMitts
	{
		public override int LabelNumber { get { return 1070924; } } // Gloves of the Sun

		public override int BaseFireResistance { get { return 24; } }

		[Constructable]
		public GlovesOfTheSun() : base()
		{
			Attributes.RegenHits = 2;
			Attributes.NightSight = 1;
			Attributes.LowerManaCost = 5;
			Attributes.LowerRegCost = 18;
		}

		public GlovesOfTheSun( Serial serial ) : base( serial )
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

	public class HanzosBow : Yumi
	{
		public override int LabelNumber { get { return 1070918; } } // Hanzo's Bow

		[Constructable]
		public HanzosBow() : base()
		{
			WeaponAttributes.HitLeechHits = 40;
			WeaponAttributes.SelfRepair = 3;

			Attributes.WeaponDamage = 50;

			SkillBonuses.SetValues( 0, SkillName.Ninjitsu, 10 );
		}

		public HanzosBow( Serial serial ) : base( serial )
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

	public class LegsOfStability : PlateSuneate
	{
		public override int LabelNumber { get { return 1070925; } } // Legs of Stability

		public override int BasePhysicalResistance { get { return 20; } }
		public override int BasePoisonResistance { get { return 18; } }

		[Constructable]
		public LegsOfStability() : base()
		{
			Attributes.BonusStam = 5;

			ArmorAttributes.SelfRepair = 3;
			ArmorAttributes.LowerStatReq = 100;
			ArmorAttributes.MageArmor = 1;
		}

		public LegsOfStability( Serial serial ) : base( serial )
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

	public class PeasantsBokuto : Bokuto
	{
		public override int LabelNumber { get { return 1070912; } } // Peasant's Bokuto

		[Constructable]
		public PeasantsBokuto() : base()
		{
			WeaponAttributes.SelfRepair = 3;
			WeaponAttributes.HitLowerDefend = 30;

			Attributes.WeaponDamage = 35;
			Attributes.WeaponSpeed = 10;
			Slayer = SlayerName.SnakesBane;
		}

		public PeasantsBokuto( Serial serial ) : base( serial )
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

	public class PilferedDancerFans : Tessen
	{
		public override int LabelNumber { get { return 1070916; } } // Pilfered Dancer Fans

		[Constructable]
		public PilferedDancerFans() : base()
		{
			Attributes.WeaponDamage = 20;
			Attributes.WeaponSpeed = 20;
			Attributes.CastRecovery = 2;
			Attributes.DefendChance = 5;
			Attributes.SpellChanneling = 1;
		}

		public PilferedDancerFans( Serial serial ) : base( serial )
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

	public class TheDestroyer : NoDachi
	{
		public override int LabelNumber { get { return 1070915; } } // The Destroyer

		[Constructable]
		public TheDestroyer() : base()
		{
			WeaponAttributes.HitLeechStam = 40;

			Attributes.BonusStr = 6;
			Attributes.AttackChance = 10;
			Attributes.WeaponDamage = 50;
		}

		public TheDestroyer( Serial serial ) : base( serial )
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

	public class TomeOfEnlightenment : Spellbook
	{
		public override int LabelNumber { get { return 1070934; } } // Tome of Enlightenment

		[Constructable]
		public TomeOfEnlightenment() : base()
		{
			LootType = LootType.Regular;
			Hue = 0x455;

			Attributes.BonusInt = 5;
			Attributes.SpellDamage = 10;
			Attributes.CastSpeed = 1;
		}

		public TomeOfEnlightenment( Serial serial ) : base( serial )
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

	//Non weapon/armor ones:

	public class AncientUrn : Item
	{
		private static string[] m_Names = new string[]
			{
				"Akira",
				"Avaniaga",
				"Aya",
				"Chie",
				"Emiko",
				"Fumiyo",
				"Gennai",
				"Gennosuke", 
				"Genjo",
				"Hamato",
				"Harumi",
				"Ikuyo",
				"Juri",
				"Kaori",
				"Kaoru",
				"Kiyomori",
				"Mayako",
				"Motoki",
				"Musashi",
				"Nami",
				"Nobukazu",
				"Roku",
				"Romi",
				"Ryo",
				"Sanzo",
				"Sakamae",
				"Satoshi",
				"Takamori",
				"Takuro",
				"Teruyo",
				"Toshiro",
				"Yago",
				"Yeijiro",
				"Yoshi",
				"Zeshin"
			};

		public static string[] Names { get { return m_Names; } }

		private string m_UrnName;

		[CommandProperty( AccessLevel.GameMaster )]
		public string UrnName
		{
			get { return m_UrnName; }
			set { m_UrnName = value; }
		}

		public override int LabelNumber { get { return 1071014; } } // Ancient Urn

		[Constructable]
		public AncientUrn( string urnName ) : base( 0x241D )
		{
			m_UrnName = urnName;
			Weight = 1.0;
		}

		[Constructable]
		public AncientUrn() : this( m_Names[Utility.Random( m_Names.Length )] )
		{
		}

		public AncientUrn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
			writer.Write( m_UrnName );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			m_UrnName = reader.ReadString();

			Utility.Intern( ref m_UrnName );
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			list.Add( 1070935, m_UrnName ); // Ancient Urn of ~1_name~
		}

		public override void OnSingleClick( Mobile from )
		{
			LabelTo( from, 1070935, m_UrnName ); // Ancient Urn of ~1_name~
		}

	}

	public class HonorableSwords : Item
	{
		private string m_SwordsName;

		[CommandProperty( AccessLevel.GameMaster )]
		public string SwordsName
		{
			get { return m_SwordsName; }
			set { m_SwordsName = value; }
		}

		public override int LabelNumber { get { return 1071015; } } // Honorable Swords

		[Constructable]
		public HonorableSwords( string swordsName ) : base( 0x2853 )
		{
			m_SwordsName = swordsName;

			Weight = 5.0;
		}

		[Constructable]
		public HonorableSwords() : this( AncientUrn.Names[Utility.Random( AncientUrn.Names.Length )] )
		{
		}

		public HonorableSwords( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
			writer.Write( m_SwordsName );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			m_SwordsName = reader.ReadString();

			Utility.Intern( ref m_SwordsName );
		}

		public override void AddNameProperty( ObjectPropertyList list )
		{
			list.Add( 1070936, m_SwordsName ); // Honorable Swords of ~1_name~
		}

		public override void OnSingleClick( Mobile from )
		{
			LabelTo( from, 1070936, m_SwordsName ); // Honorable Swords of ~1_name~
		}

		//TODO: the Chest of Heirlooms
	}

	public class FluteOfRenewal : BambooFlute
	{
		public override int LabelNumber { get { return 1070927; } } // Flute of Renewal

		[Constructable]
		public FluteOfRenewal() : base()
		{
			Slayer = SlayerGroup.Groups[Utility.Random( SlayerGroup.Groups.Length - 1 )].Super.Name; //-1 to exclude Fey slayer.  Try to confrim no fey slayer on this on OSI

			ReplenishesCharges = true;
		}

		public override int InitMinUses { get { return 300; } }
		public override int InitMaxUses { get { return 300; } }

		public FluteOfRenewal( Serial serial ) : base( serial )
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

			if( version == 0 && Slayer == SlayerName.Fey )
				Slayer = SlayerGroup.Groups[Utility.Random( SlayerGroup.Groups.Length - 1 )].Super.Name;
		}
	}
}