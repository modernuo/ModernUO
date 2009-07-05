using System;
using System.Collections;
using Server.Items;
using Server.Targeting;
using Server.Misc;

namespace Server.Mobiles
{
	[CorpseName("a satyr corpse")]
	public class Satyr : BaseCreature
	{
		/* These creatures should be able to cast Area Peace as 
		 * well as cause Players' armor to drop into their backpack
		 */
		
		public override WeaponAbility GetWeaponAbility()
		{
			return WeaponAbility.Disrobe;
		}
		
		[Constructable]
		public Satyr()
			: base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4) // NEED TO CHECK
		{
			Name = "a satyr";
			Body = 271;
			BaseSoundID = 0x586;  

			SetStr(177, 195);
			SetDex(251, 269);
			SetInt(153, 170);

			SetHits(353, 399);

			SetDamage(13, 24);

			SetDamageType(ResistanceType.Physical, 100);

			SetResistance(ResistanceType.Physical, 55, 60);
			SetResistance(ResistanceType.Fire, 26, 35);
			SetResistance(ResistanceType.Cold, 30, 40);
			SetResistance(ResistanceType.Poison, 30, 40);
			SetResistance(ResistanceType.Energy, 30, 40);

			SetSkill(SkillName.Poisoning, 0);
			SetSkill(SkillName.Meditation, 0);
			SetSkill(SkillName.EvalInt, 0);
			SetSkill(SkillName.Magery, 0);
			SetSkill(SkillName.Anatomy, 0);
			SetSkill(SkillName.MagicResist, 55.3, 64.3);
			SetSkill(SkillName.Tactics, 80.1, 99.3);
			SetSkill(SkillName.Wrestling, 80.6, 100.0);

			Fame = 5000;
			Karma = -5000;

			VirtualArmor = 28; // Don't know what it should be
		}


		public override void GenerateLoot()
		{
			AddLoot(LootPack.Rich);  // Need to verify
		}

		public override int Meat { get { return 1; } }

		public Satyr(Serial serial) : base(serial)
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
