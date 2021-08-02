using System;
using Server;
using Xanthos.Interfaces;

namespace EvolutionPetSystem
{
	public abstract class BaseEvoSpec
	{
		public bool CanAttackPlayers { get { return m_CanAttackPlayers; } }
		public bool Tamable { get { return m_Tamable; } }
		public double MinTamingToHatch { get { return m_MinTamingToHatch; } }
		public double GuardianEggOrDeedChance { get { return m_GuardianEggOrDeedChance; } }
		public bool AlwaysHappy { get { return m_AlwaysHappy; } }
		public int MaxEvoResistance { get { return m_MaxEvoResistance; } }
		public SkillName [] Skills { get { return m_Skills; } }
		public int [] MinSkillValues { get { return m_MinSkillValues; } }
		public int [] MaxSkillValues { get { return m_MaxSkillValues; } }
		public BaseEvoStage [] Stages { get { return m_Stages; } }
		public double HatchDuration { get { return m_HatchDuration; } }

		protected BaseEvoSpec() { }

		protected bool m_CanAttackPlayers;			// Keep things fair?
		protected bool m_Tamable;					// Is it or not?
		protected double m_MinTamingToHatch;		// Skill required - independent of requirement to tame one gone wild
		protected double m_GuardianEggOrDeedChance;	// Chance to produce an egg or deed as loot - Guardians only
		protected bool m_AlwaysHappy;				// Keeps it wonderfully happy if true, otherwise needs food like other pets
		protected int m_MaxEvoResistance;			// The cap even with mods (i.e. armor, et.) on.
		protected SkillName [] m_Skills;			// List of skill names and min/max values
		protected int [] m_MinSkillValues;			// a skill's min-value
		protected int [] m_MaxSkillValues;			// a skill's max-value
		protected BaseEvoStage [] m_Stages;			// The list of stages.
		protected double m_HatchDuration;			// Days of egg incubation time (1.00 = one hour, 0.25 = 15 minutes).
	}

	public abstract class BaseEvoStage
	{
		public string Title;
		public string EvolutionMessage;
		public int NextEpThreshold;
		public int EpMinDivisor;
		public int EpMaxDivisor;
		public int DustMultiplier;
		public int BaseSoundID;
		public int BodyValue;
		public int ControlSlots;
		public double MinTameSkill;
		public int VirtualArmor;
		public int Hue;

		public ResistanceType [] DamagesTypes;
		public int [] MinDamages;
		public int [] MaxDamages;

		public ResistanceType [] ResistanceTypes;
		public int [] MinResistances;
		public int [] MaxResistances;

		public int DamageMin;
		public int DamageMax;
		public int StrMin;
		public int StrMax;
		public int DexMin;
		public int DexMax;
		public int IntMin;
		public int IntMax;
	}
}
