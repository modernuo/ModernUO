using System;
using Server.Items;
using Server.Targeting;
using System.Collections;
using EvolutionPetSystem;

namespace Server.Mobiles
{
	[CorpseName( "a spider queen corpse" )] // stupid corpse name
	public class SpiderQueen : BaseCreature
	{
		[Constructible]
		public SpiderQueen() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.1 )
		{
			Name = "a spider queen";
			Body =  0xAD;
			BaseSoundID = 0x388; // TODO: validate

			SetStr( 796, 825 );
			SetDex( 86, 105 );
			SetInt( 436, 475 );

			SetHits( 3000, 3800 );

			SetDamage( 16, 22 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 55, 65 );
			SetResistance( ResistanceType.Fire, 60, 70 );
			SetResistance( ResistanceType.Cold, 30, 40 );
			SetResistance( ResistanceType.Poison, 25, 35 );
			SetResistance( ResistanceType.Energy, 35, 45 );

			SetSkill( SkillName.EvalInt, 30.1, 40.0 );
			SetSkill( SkillName.Magery, 30.1, 40.0 );
			SetSkill( SkillName.MagicResist, 99.1, 100.0 );
			SetSkill( SkillName.Tactics, 97.6, 100.0 );
			SetSkill( SkillName.Wrestling, 90.1, 92.5 );

			Fame = 15000;
			Karma = -15000;

			VirtualArmor = 24;

			PackItem( new SpidersSilk( 200 ) );
			PackItem( new LesserPoisonPotion() );
			PackItem( new LesserPoisonPotion() );
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich, 2 );
			AddLoot( LootPack.Gems, 8 );
		}

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (0.10 > Utility.RandomDouble())
                c.DropItem(new SpiderEgg());
            
            if (0.20 > Utility.RandomDouble())
                c.DropItem(new SpiderDust());
            
            if (0.15 > Utility.RandomDouble())
            {
                switch (Utility.Random(24))
                {
                    case 0:
                    default: c.DropItem(new MJBofInvisibility()); break;
                    case 1: c.DropItem(new MJBofAgility()); break;
                    case 2: c.DropItem(new MJBofCunning()); break;
                    case 3: c.DropItem(new MJBofStrength()); break;
                    case 4: c.DropItem(new MJBofTeleport()); break;
                    case 5: c.DropItem(new MJBofBless()); break;
                    case 6: c.DropItem(new MJRofInvisibility()); break;
                    case 7: c.DropItem(new MJRofAgility()); break;
                    case 8: c.DropItem(new MJRofCunning()); break;
                    case 9: c.DropItem(new MJRofStrength()); break;
                    case 10: c.DropItem(new MJRofTeleport()); break;
                    case 11: c.DropItem(new MJRofBless()); break;
                    case 12: c.DropItem(new MJNofInvisibility()); break;
                    case 13: c.DropItem(new MJNofAgility()); break;
                    case 14: c.DropItem(new MJNofCunning()); break;
                    case 15: c.DropItem(new MJNofStrength()); break;
                    case 16: c.DropItem(new MJNofTeleport()); break;
                    case 17: c.DropItem(new MJNofBless()); break;
                    case 18: c.DropItem(new MJEofInvisibility()); break;
                    case 19: c.DropItem(new MJEofAgility()); break;
                    case 20: c.DropItem(new MJEofCunning()); break;
                    case 21: c.DropItem(new MJEofStrength()); break;
                    case 22: c.DropItem(new MJEofTeleport()); break;
                    case 23: c.DropItem(new MJEofBless()); break;
                }
            }
        }

		public override FoodType FavoriteFood{ get{ return FoodType.Meat; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }
		public override Poison HitPoison{ get{ return Poison.Lethal; } }

		public SpiderQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}

		public void SpawnGiantSpider( Mobile m )
		{
			Map map = this.Map;

			if ( map == null )
				return;

			GiantSpider spawned = new GiantSpider();

			spawned.Team = this.Team;

			bool validLocation = false;
			Point3D loc = this.Location;

			for ( int j = 0; !validLocation && j < 10; ++j )
			{
				int x = X + Utility.Random( 3 ) - 1;
				int y = Y + Utility.Random( 3 ) - 1;
				int z = map.GetAverageZ( x, y );

				if ( validLocation = map.CanFit( x, y, this.Z, 16, false, false ) )
					loc = new Point3D( x, y, Z );
				else if ( validLocation = map.CanFit( x, y, z, 16, false, false ) )
					loc = new Point3D( x, y, z );
			}

			spawned.MoveToWorld( loc, map );
			spawned.Combatant = m;
		}

		public void EatGiantSpider()
		{
			ArrayList toEat = new ArrayList();
  
			foreach ( Mobile m in this.GetMobilesInRange( 2 ) )
			{
				if ( m is GiantSpider )
					toEat.Add( m );
			}

			if ( toEat.Count > 0 )
			{
				PlaySound( Utility.Random( 0x3B, 2 ) ); // Eat sound

				foreach ( Mobile m in toEat )
				{
					Hits += (m.Hits / 2);
					m.Delete();
				}
			}
		}

		public override void OnGotMeleeAttack( Mobile attacker )
		{
			base.OnGotMeleeAttack( attacker );

			if ( this.Hits > (this.HitsMax / 4) )
			{
				if ( 0.25 >= Utility.RandomDouble() )
					SpawnGiantSpider( attacker );
			}
			else if ( 0.25 >= Utility.RandomDouble() )
			{
				EatGiantSpider();
			}
		}
	}
}
