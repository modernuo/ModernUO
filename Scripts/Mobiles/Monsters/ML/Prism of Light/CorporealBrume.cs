using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "a corporeal brume corpse" )]
	public class CorporealBrume : BaseCreature
	{
		[Constructable]
		public CorporealBrume()
			: base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a corporeal brume";
			Body = 0x104; // TODO: Verify
			BaseSoundID = 0x56B;

			SetStr( 400, 450 );
			SetDex( 100, 150 );
			SetInt( 50, 60 );

			SetHits( 1150, 1250 );

			SetDamage( 21, 25 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 100 );
			SetResistance( ResistanceType.Fire, 40, 50 );
			SetResistance( ResistanceType.Cold, 40, 50 );
			SetResistance( ResistanceType.Poison, 50, 60 );
			SetResistance( ResistanceType.Energy, 30, 40 );

			SetSkill( SkillName.Wrestling, 110.0, 115.0 );
			SetSkill( SkillName.Tactics, 110.0, 115.0 );
			SetSkill( SkillName.MagicResist, 80.0, 95.0 );
			SetSkill( SkillName.Anatomy, 100.0, 110.0 );

			Fame = 12000;
			Karma = -12000;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.FilthyRich );
			AddLoot( LootPack.Rich );
		}

		// TODO: Verify area attack specifics
		public override bool HasAura { get { return ( Combatant != null ); } }
		public override TimeSpan AuraInterval { get { return TimeSpan.FromSeconds( 20 ); } }
		public override int AuraRange { get { return 10; } }

		public override int AuraBaseDamage { get { return Utility.RandomMinMax( 25, 35 ); } }
		public override int AuraFireDamage { get { return 0; } }
		public override int AuraColdDamage { get { return 100; } }

		public override void AuraEffect( Mobile m )
		{
			m.FixedParticles( 0x374A, 10, 15, 5038, 1181, 2, EffectLayer.Head );
			m.PlaySound( 0x213 );
		}

		public CorporealBrume( Serial serial )
			: base( serial )
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
}