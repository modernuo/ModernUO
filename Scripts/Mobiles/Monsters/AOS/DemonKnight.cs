using System;
using System.Collections;
using Server;
using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{
	[CorpseName( "a demon knight corpse" )]
	public class DemonKnight : BaseCreature
	{
		public override bool IgnoreYoungProtection { get { return Core.ML; } }

		public static Type[] ArtifactRarity10 { get { return m_ArtifactRarity10; } }
		public static Type[] ArtifactRarity11 { get { return m_ArtifactRarity11; } }
		private static Type[] m_ArtifactRarity10 = new Type[]
			{
				typeof( TheTaskmaster )
			};

		private static Type[] m_ArtifactRarity11 = new Type[]
			{
				typeof( LegacyOfTheDreadLord ),
				typeof( TheDragonSlayer ),
				typeof( ArmorOfFortune ),
				typeof( GauntletsOfNobility ),
				typeof( HelmOfInsight ),
				typeof( HolyKnightsBreastplate ),
				typeof( JackalsCollar ),
				typeof( LeggingsOfBane ),
				typeof( MidnightBracers ),
				typeof( OrnateCrownOfTheHarrower ),
				typeof( ShadowDancerLeggings ),
				typeof( TunicOfFire ),
				typeof( VoiceOfTheFallenKing ),
				typeof( BraceletOfHealth ),
				typeof( OrnamentOfTheMagician ),
				typeof( RingOfTheElements ),
				typeof( RingOfTheVile ),
				typeof( Aegis ),
				typeof( ArcaneShield ),
				typeof( AxeOfTheHeavens ),
				typeof( BladeOfInsanity ),
				typeof( BoneCrusher ),
				typeof( BreathOfTheDead ),
				typeof( Frostbringer ),
				typeof( SerpentsFang ),
				typeof( StaffOfTheMagi ),
				typeof( TheBeserkersMaul ),
				typeof( TheDryadBow ),
				typeof( DivineCountenance ),
				typeof( HatOfTheMagi ),
				typeof( HuntersHeaddress ),
				typeof( SpiritOfTheTotem )
			};

		public static Item CreateRandomArtifact()
		{
			if ( !Core.AOS )
				return null;

			int count = ( m_ArtifactRarity10.Length * 5 ) + ( m_ArtifactRarity11.Length * 4 );
			int random = Utility.Random( count );
			Type type;

			if ( random < ( m_ArtifactRarity10.Length * 5 ) )
			{
				type = m_ArtifactRarity10[random / 5];
			}
			else
			{
				random -= m_ArtifactRarity10.Length * 5;
				type = m_ArtifactRarity11[random / 4];
			}

			return Loot.Construct( type );
		}

		public static Mobile FindRandomPlayer( BaseCreature creature )
		{
			List<DamageStore> rights = BaseCreature.GetLootingRights( creature.DamageEntries, creature.HitsMax );

			for ( int i = rights.Count - 1; i >= 0; --i )
			{
				DamageStore ds = rights[i];

				if ( !ds.m_HasRight )
					rights.RemoveAt( i );
			}

			if ( rights.Count > 0 )
				return rights[Utility.Random( rights.Count )].m_Mobile;

			return null;
		}

		public static void DistributeArtifact( BaseCreature creature )
		{
			DistributeArtifact( creature, CreateRandomArtifact() );
		}

		public static void DistributeArtifact( BaseCreature creature, Item artifact )
		{
			DistributeArtifact( FindRandomPlayer( creature ), artifact );
		}

		public static void DistributeArtifact( Mobile to )
		{
			DistributeArtifact( to, CreateRandomArtifact() );
		}

		public static void DistributeArtifact( Mobile to, Item artifact )
		{
			if ( to == null || artifact == null )
				return;

			Container pack = to.Backpack;

			if ( pack == null || !pack.TryDropItem( to, artifact, false ) )
				to.BankBox.DropItem( artifact );

			to.SendLocalizedMessage( 1062317 ); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
		}

		public static int GetArtifactChance( Mobile boss )
		{
			if ( !Core.AOS )
				return 0;

			int luck = LootPack.GetLuckChanceForKiller( boss );
			int chance;

			if ( boss is DemonKnight )
				chance = 1500 + (luck / 5);
			else
				chance = 750 + (luck / 10);

			return chance;
		}

		public static bool CheckArtifactChance( Mobile boss )
		{
			return GetArtifactChance( boss ) > Utility.Random( 100000 );
		}

		public override WeaponAbility GetWeaponAbility()
		{
			switch ( Utility.Random( 3 ) )
			{
				default:
				case 0: return WeaponAbility.DoubleStrike;
				case 1: return WeaponAbility.WhirlwindAttack;
				case 2: return WeaponAbility.CrushingBlow;
			}
		}

		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( !Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance( this ) )
				DemonKnight.DistributeArtifact( this );
		}

		[Constructable]
		public DemonKnight() : base( AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = NameList.RandomName( "demon knight" );
			Title = "the Dark Father";
			Body = 318;
			BaseSoundID = 0x165;

			SetStr( 500 );
			SetDex( 100 );
			SetInt( 1000 );

			SetHits( 30000 );
			SetMana( 5000 );

			SetDamage( 17, 21 );

			SetDamageType( ResistanceType.Physical, 20 );
			SetDamageType( ResistanceType.Fire, 20 );
			SetDamageType( ResistanceType.Cold, 20 );
			SetDamageType( ResistanceType.Poison, 20 );
			SetDamageType( ResistanceType.Energy, 20 );

			SetResistance( ResistanceType.Physical, 30 );
			SetResistance( ResistanceType.Fire, 30 );
			SetResistance( ResistanceType.Cold, 30 );
			SetResistance( ResistanceType.Poison, 30 );
			SetResistance( ResistanceType.Energy, 30 );

			SetSkill( SkillName.DetectHidden, 80.0 );
			SetSkill( SkillName.EvalInt, 100.0 );
			SetSkill( SkillName.Magery, 100.0 );
			SetSkill( SkillName.Meditation, 120.0 );
			SetSkill( SkillName.MagicResist, 150.0 );
			SetSkill( SkillName.Tactics, 100.0 );
			SetSkill( SkillName.Wrestling, 120.0 );

			Fame = 28000;
			Karma = -28000;

			VirtualArmor = 64;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.SuperBoss, 2 );
			AddLoot( LootPack.HighScrolls, Utility.RandomMinMax( 6, 60 ) );
		}

		public override bool BardImmune{ get{ return !Core.SE; } }
		public override bool Unprovokable{ get{ return Core.SE; } }
		public override bool Uncalmable{ get{ return Core.SE; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }

		public override int TreasureMapLevel{ get{ return 1; } }

		private static bool m_InHere;

		public override void OnDamage( int amount, Mobile from, bool willKill )
		{
			if ( from != null && from != this && !m_InHere )
			{
				m_InHere = true;
				AOS.Damage( from, this, Utility.RandomMinMax( 8, 20 ), 100, 0, 0, 0, 0 );

				MovingEffect( from, 0xECA, 10, 0, false, false, 0, 0 );
				PlaySound( 0x491 );

				if ( 0.05 > Utility.RandomDouble() )
					Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( CreateBones_Callback ), from );

				m_InHere = false;
			}
		}

		public virtual void CreateBones_Callback( object state )
		{
			Mobile from = (Mobile)state;
			Map map = from.Map;

			if ( map == null )
				return;

			int count = Utility.RandomMinMax( 1, 3 );

			for ( int i = 0; i < count; ++i )
			{
				int x = from.X + Utility.RandomMinMax( -1, 1 );
				int y = from.Y + Utility.RandomMinMax( -1, 1 );
				int z = from.Z;

				if ( !map.CanFit( x, y, z, 16, false, true ) )
				{
					z = map.GetAverageZ( x, y );

					if ( z == from.Z || !map.CanFit( x, y, z, 16, false, true ) )
						continue;
				}

				UnholyBone bone = new UnholyBone();

				bone.Hue = 0;
				bone.Name = "unholy bones";
				bone.ItemID = Utility.Random( 0xECA, 9 );

				bone.MoveToWorld( new Point3D( x, y, z ), map );
			}
		}

		public DemonKnight( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}