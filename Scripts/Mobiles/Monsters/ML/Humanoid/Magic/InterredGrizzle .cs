using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName("an interred grizzle corpse")]
	public class InterredGrizzle  : BaseCreature
	{
		[Constructable]
		public  InterredGrizzle () : base( AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "an interred grizzle";
			Body = 259;

			SetStr( 451, 500 );
			SetDex( 201, 250 );
			SetInt( 801, 850 );

			SetHits( 1500 );
			SetStam( 150 );

			SetDamage( 16, 19 );

			SetDamageType( ResistanceType.Physical, 30 );
			SetDamageType( ResistanceType.Fire, 70 );

			SetResistance( ResistanceType.Physical, 35, 55 );
			SetResistance( ResistanceType.Fire, 20, 65 );
			SetResistance( ResistanceType.Cold, 55, 80 );
			SetResistance( ResistanceType.Poison, 20, 35 );
			SetResistance( ResistanceType.Energy, 60, 80 );

			SetSkill(SkillName.Meditation, 77.7, 84.0 );
			SetSkill(SkillName.EvalInt, 72.2, 79.6 );
			SetSkill(SkillName.Magery, 83.7, 89.6);
			SetSkill(SkillName.Poisoning, 0 );
			SetSkill(SkillName.Anatomy, 0 );
			SetSkill( SkillName.MagicResist, 80.2, 87.3 );
			SetSkill( SkillName.Tactics, 104.5, 105.1 );
			SetSkill( SkillName.Wrestling, 105.1, 109.4 );

			Fame = 3700;  // Guessed
			Karma = -3700;  // Guessed
		}

		public override void GenerateLoot() // -- Need to verify
		{
			AddLoot( LootPack.FilthyRich );
		}

		// TODO: Acid Blood
		/*
		 * Message: 1070820
		 * Spits pool of acid (blood, hue 0x3F), hits lost 6-10 per second/step
		 * Damage is resistable (physical)
		 * Acid last 10 seconds
		 */
		 
		public override int GetAngerSound()
		{
			return 0x581;
		}

		public override int GetIdleSound()
		{
			return 0x582;
		}

		public override int GetAttackSound()
		{
			return 0x580;
		}

		public override int GetHurtSound()
		{
			return 0x583;
		}

		public override int GetDeathSound()
		{
			return 0x584;
		}

		public override void OnDamage( int amount, Mobile from, bool willKill )
		{
			if( Utility.RandomDouble() < 0.1 )
				DropOoze();

			base.OnDamage( amount, from, willKill );
		}

		private int RandomPoint( int mid )
		{
			return ( mid + Utility.RandomMinMax( -2, 2 ) );
		}

		public virtual Point3D GetSpawnPosition( int range )
		{
			return GetSpawnPosition( Location, Map, range );
		}

		public virtual Point3D GetSpawnPosition( Point3D from, Map map, int range )
		{
			if( map == null )
				return from;

			Point3D loc = new Point3D( ( RandomPoint( X ) ), ( RandomPoint( Y ) ), Z );

			loc.Z = Map.GetAverageZ( loc.X, loc.Y );

			return loc;
		}

		public virtual void DropOoze()
		{
			int amount = Utility.RandomMinMax( 1, 3 );
			bool corrosive = Utility.RandomBool();

			for( int i = 0; i < amount; i++ )
			{
				Item ooze = new StainedOoze( corrosive );
				Point3D p = new Point3D( Location );

				for( int j = 0; j < 5; j++ )
				{
					p = GetSpawnPosition( 2 );
					bool found = false;

					foreach( Item item in Map.GetItemsInRange( p, 0 ) )
						if( item is StainedOoze )
						{
							found = true;
							break;
						}

					if( !found )
						break;
				}

				ooze.MoveToWorld( p, Map );
			}

			if( Combatant != null )
			{
				if( corrosive )
					Combatant.SendLocalizedMessage( 1072071 ); // A corrosive gas seeps out of your enemy's skin!
				else
					Combatant.SendLocalizedMessage( 1072072 ); // A poisonous gas seeps out of your enemy's skin!
			}
		}
		/*
		public override bool OnBeforeDeath()
		{
			SpillAcid( 1, 4, 10, 6, 10 );

			return base.OnBeforeDeath();
		}
		*/

		public  InterredGrizzle ( Serial serial ) : base( serial )
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
