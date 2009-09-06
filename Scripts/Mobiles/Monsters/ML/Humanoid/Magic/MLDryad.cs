using System;
using System.Collections;
using Server.Items;
using Server.Targeting;
using Server.Misc;

namespace Server.Mobiles
{
	[CorpseName("a dryad corpse")]
	public class MLDryad : BaseCreature
	{
		public override bool InitialInnocent { get { return true; } }
	   
		/* VULNERABLE TO FEY SLAYER!
		* These creatures should be able to cast Area Peace as 
		* well as cause Players' armor to drop into their backpack*/

		public override WeaponAbility GetWeaponAbility()
		{
			return WeaponAbility.Disrobe;
		}

		[Constructable]
		public MLDryad()
			: base(AIType.AI_Mage, FightMode.Aggressor, 10, 1, 0.2, 0.4) // NEED TO CHECK
		{
			Name = "a dryad";
			Body = 266;
			BaseSoundID = 0x57B;

			SetStr(132, 149);
			SetDex(152, 168);
			SetInt(251, 280);

			SetHits(304, 321);

			SetDamage(11, 20);

			SetDamageType(ResistanceType.Physical, 100);

			SetResistance(ResistanceType.Physical, 40, 50);
			SetResistance(ResistanceType.Fire, 15, 25);
			SetResistance(ResistanceType.Cold, 40, 45);
			SetResistance(ResistanceType.Poison, 30, 40);
			SetResistance(ResistanceType.Energy, 25, 35);

			SetSkill(SkillName.Meditation, 80.9, 89.9);
			SetSkill(SkillName.EvalInt, 70.3, 78.7);
			SetSkill(SkillName.Magery, 70.7, 75.7);
			SetSkill(SkillName.Anatomy, 0);
			SetSkill(SkillName.MagicResist, 101.7, 117.1);
			SetSkill(SkillName.Tactics, 71.7, 79.8);
			SetSkill(SkillName.Wrestling, 72.5, 79.5);

			Fame = 5000;
			Karma = 5000;

			VirtualArmor = 28; // Don't know what it should be

			if ( Core.ML && Utility.RandomDouble() < .33 )
				PackItem( Engines.Plants.Seed.RandomPeculiarSeed(1) );
		}

		public override void GenerateLoot()
		{
			AddLoot(LootPack.Rich);  // Need to verify
		}

		public override int Meat { get { return 1; } }
		
		public MLDryad(Serial serial) : base(serial)
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
