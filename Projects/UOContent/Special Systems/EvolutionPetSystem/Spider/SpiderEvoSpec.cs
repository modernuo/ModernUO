using System;
using Server;

namespace EvolutionPetSystem
{
	public sealed class SpiderSpec : BaseEvoSpec
	{
		SpiderSpec()
		{
			m_CanAttackPlayers = true;
			m_Tamable = false;
			m_MinTamingToHatch = 50.0;	
			m_GuardianEggOrDeedChance = 0;
			m_AlwaysHappy = true;
			m_MaxEvoResistance = 70;
			m_Skills = new SkillName[7] { SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.MagicResist, SkillName.Tactics, SkillName.Wrestling, SkillName.Anatomy };
			m_MinSkillValues = new int[7] { 30, 20, 20, 10, 10, 10, 10 };
			m_MaxSkillValues = new int[7] { 100, 100, 100, 100, 100, 100, 100 };
            m_Stages = new BaseEvoStage[] { new SpiderStageOne(), new SpiderStageTwo(), new SpiderStageThree(), new SpiderStageFour(), new SpiderStageFive(), new SpiderStageSix(), new SpiderStageSeven() };
		}

		public static SpiderSpec Instance { get { return Nested.instance; } }
		class Nested { static Nested() { } internal static readonly SpiderSpec instance = new SpiderSpec();}
	}	

	public class SpiderStageOne : BaseEvoStage
	{
		public SpiderStageOne()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 2500000; EpMinDivisor = 10; EpMaxDivisor = 5; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 52; ControlSlots = 2; MinTameSkill = 50.0; VirtualArmor = 30;
			Hue = 1175;

			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 5 };
			MaxDamages = new int[1] { 5 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 10 };
			MaxResistances = new int[1] { 10 };

			DamageMin = 20; DamageMax = 25;
			StrMin = 500; StrMax = 535; DexMin = 90; DexMax = 160; IntMin = 130; IntMax = 250;
		}
	}

	public class SpiderStageTwo : BaseEvoStage
	{
        public SpiderStageTwo()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 5000000; EpMinDivisor = 20; EpMaxDivisor = 10; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 21; ControlSlots = 2; MinTameSkill = 60.0; VirtualArmor = 40;
			Hue = 157;
		
			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 10 };
			MaxDamages = new int[1] { 10 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 15 };
			MaxResistances = new int[1] { 15 };

            DamageMin = 24; DamageMax = 29;
			StrMin = 510; StrMax = 590; DexMin = 150; DexMax = 180; IntMin = 250; IntMax = 290;
		}
	}

	public class SpiderStageThree : BaseEvoStage
	{
        public SpiderStageThree()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 10000000; EpMinDivisor = 30; EpMaxDivisor = 20; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 28; ControlSlots = 2; MinTameSkill = 80.0; VirtualArmor = 50;
			Hue = 20000;

			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 12 };
			MaxDamages = new int[1] { 12 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 30 };
			MaxResistances = new int[1] { 30 };

            DamageMin = 22; DamageMax = 32;
			StrMin = 520; StrMax = 600; DexMin = 180; DexMax = 200; IntMin = 310; IntMax = 350;
		}
	}

	public class SpiderStageFour : BaseEvoStage
	{
        public SpiderStageFour()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 20000000; EpMinDivisor = 50; EpMaxDivisor = 40; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 28; ControlSlots = 3; MinTameSkill = 100.0; VirtualArmor = 60;
			Hue = 1175;
		
			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 13 };
			MaxDamages = new int[1] { 13 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 40 };
			MaxResistances = new int[1] { 40 };

			DamageMin = 22; DamageMax = 32;
			StrMin = 600; StrMax = 630; DexMin = 190; DexMax = 205; IntMin = 322; IntMax = 388;
		}
	}

	public class SpiderStageFive : BaseEvoStage
	{
        public SpiderStageFive()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 40000000; EpMinDivisor = 160; EpMaxDivisor = 40; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 11; ControlSlots = 3; MinTameSkill = 110.0; VirtualArmor = 70;
			Hue = 1175;
		
			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 14 };
			MaxDamages = new int[1] { 14 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 50 };
			MaxResistances = new int[1] { 50 };

			DamageMin = 27; DamageMax = 36;
			StrMin = 630; StrMax = 710; DexMin = 200; DexMax = 210; IntMin = 399; IntMax = 410;
		}
	}

	public class SpiderStageSix : BaseEvoStage
	{
		public SpiderStageSix()
		{
			Title = "";
			EvolutionMessage = "has evolved";
			NextEpThreshold = 80000000; EpMinDivisor = 540; EpMaxDivisor = 480; DustMultiplier = 20000;
            BaseSoundID = 387; BodyValue = 11; ControlSlots = 4; MinTameSkill = 115.0; VirtualArmor = 90;
			Hue = 2101;

			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 15 };
			MaxDamages = new int[1] { 15 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 60 };
			MaxResistances = new int[1] { 60 };

			DamageMin = 30; DamageMax = 37;
			StrMin = 710; StrMax = 800; DexMin = 230; DexMax = 250; IntMin = 450; IntMax = 460;
		}
	}

	public class SpiderStageSeven : BaseEvoStage
	{
		public SpiderStageSeven()
		{
			Title = "the ancient spider";
			EvolutionMessage = "has evolved to its highest form and is now an Ancient Spider";
			NextEpThreshold = 88000000; EpMinDivisor = 740; EpMaxDivisor = 660; DustMultiplier = 20000;
			BaseSoundID = 387; BodyValue = 173; ControlSlots = 4; MinTameSkill = 120.0; VirtualArmor = 110;
			Hue = 2101;
		
			DamagesTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinDamages = new int[1] { 16 };
			MaxDamages = new int[1] { 16 };

			ResistanceTypes = new ResistanceType[1] { ResistanceType.Physical };
			MinResistances = new int[1] { 70 };
			MaxResistances = new int[1] { 70 };

			DamageMin = 33; DamageMax = 42;
			StrMin = 910; StrMax = 1100; DexMin = 277; DexMax = 290; IntMin = 482; IntMax = 511;
		}
	}
}
