using System;
using Server;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName("corpse of Ilhenir")]
	public class Ilhenir : BaseChampion
	{
		public override ChampionSkullType SkullType{ get{ return ChampionSkullType.Pain; } }

		public override Type[] UniqueList{ get{ return new Type[] { }; } }
		public override Type[] SharedList{ get{ return new Type[] { 	typeof( ANecromancerShroud ),
										typeof( LieutenantOfTheBritannianRoyalGuard ),
										typeof( OblivionsNeedle ),
										typeof( TheRobeOfBritanniaAri ) }; } }
		public override Type[] DecorativeList{ get{ return new Type[] { typeof( MonsterStatuette ) }; } }

		public override MonsterStatuetteType[] StatueTypes{ get{ return new MonsterStatuetteType[] { 	MonsterStatuetteType.PlagueBeast,
														MonsterStatuetteType.RedDeath }; } }

		// Based off of the Bone Demon, since Stratics and UOGuide are lacking in info. Many things guessed for now.
		[Constructable]
		public Ilhenir() : base(AIType.AI_Mage)
		{
			Name = "Ilhenir";
			Title = "the Stained";
			Body = 259;
			
			SetStr( 1105, 1350 );
			SetDex( 82, 160 );
			SetInt( 505, 750 );

			SetHits( 9000 );

			SetDamage(21, 28);

			SetDamageType(ResistanceType.Physical, 60);
			SetDamageType(ResistanceType.Fire, 20);
			SetDamageType(ResistanceType.Poison, 20);

			SetResistance(ResistanceType.Physical, 55, 65);
			SetResistance(ResistanceType.Fire, 50, 60);
			SetResistance(ResistanceType.Cold, 55, 65);
			SetResistance(ResistanceType.Poison, 70, 90);
			SetResistance(ResistanceType.Energy, 65, 75);

			SetSkill(SkillName.EvalInt, 100);
			SetSkill(SkillName.Magery, 100);
			SetSkill(SkillName.Meditation, 0);
			SetSkill(SkillName.Poisoning, 5.4);
			SetSkill(SkillName.Anatomy, 117.5);
			SetSkill(SkillName.MagicResist, 120.0);
			SetSkill(SkillName.Tactics, 119.9);  
			SetSkill(SkillName.Wrestling, 119.9);

			Fame = 50000;
			Karma = -50000;

			VirtualArmor = 44;
		}

		public override void GenerateLoot()
		{
			AddLoot(LootPack.FilthyRich, 8);
		}

		public override bool Unprovokable { get { return true; } }
		public override bool Uncalmable { get { return true; } }
		public override Poison PoisonImmune { get { return Poison.Lethal; } }
		public override int TreasureMapLevel { get { return 1; } }

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
		public Ilhenir(Serial serial)
			: base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int)0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
		}
	}
}
