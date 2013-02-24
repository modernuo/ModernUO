using System;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Engines.MLQuests.Objectives
{
	public enum GainSkillObjectiveFlags : byte
	{
		None		= 0x00,
		UseReal		= 0x01,
		Accelerate	= 0x02
	}

	public class GainSkillObjective : BaseObjective
	{
		private SkillName m_Skill;
		private int m_ThresholdFixed;
		private GainSkillObjectiveFlags m_Flags;

		public SkillName Skill
		{
			get { return m_Skill; }
			set { m_Skill = value; }
		}

		public int ThresholdFixed
		{
			get { return m_ThresholdFixed; }
			set { m_ThresholdFixed = value; }
		}

		public bool UseReal
		{
			get { return GetFlag( GainSkillObjectiveFlags.UseReal ); }
			set { SetFlag( GainSkillObjectiveFlags.UseReal, value ); }
		}

		public bool Accelerate
		{
			get { return GetFlag( GainSkillObjectiveFlags.Accelerate ); }
			set { SetFlag( GainSkillObjectiveFlags.Accelerate, value ); }
		}

		public GainSkillObjective()
			: this( SkillName.Alchemy, 0 )
		{
		}

		public GainSkillObjective( SkillName skill, int thresholdFixed )
			: this( skill, thresholdFixed, false, false )
		{
		}

		public GainSkillObjective( SkillName skill, int thresholdFixed, bool useReal, bool accelerate )
		{
			m_Skill = skill;
			m_ThresholdFixed = thresholdFixed;
			m_Flags = GainSkillObjectiveFlags.None;

			if ( useReal )
				m_Flags |= GainSkillObjectiveFlags.UseReal;

			if ( accelerate )
				m_Flags |= GainSkillObjectiveFlags.Accelerate;
		}

		public override bool CanOffer( IQuestGiver quester, PlayerMobile pm, bool message )
		{
			Skill skill = pm.Skills[m_Skill];

			if ( ( UseReal ? skill.Fixed : skill.BaseFixedPoint ) >= m_ThresholdFixed )
			{
				if ( message )
					MLQuestSystem.Tell( quester, pm, 1077772 ); // I cannot teach you, for you know all I can teach!

				return false;
			}

			return true;
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			int skillLabel = AosSkillBonuses.GetLabel( m_Skill );
			string args;

			if ( m_ThresholdFixed % 10 == 0 )
				args = String.Format( "#{0}\t{1}", skillLabel, m_ThresholdFixed / 10 ); // as seen on OSI
			else
				args = String.Format( "#{0}\t{1:0.0}", skillLabel, (double)m_ThresholdFixed / 10 ); // for non-integer skill levels

			g.AddHtmlLocalized( 98, y, 312, 16, 1077485, args, 0x15F90, false, false ); // Increase ~1_SKILL~ to ~2_VALUE~
			y += 16;
		}

		public override BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			return new GainSkillObjectiveInstance( this, instance );
		}

		private bool GetFlag( GainSkillObjectiveFlags flag )
		{
			return ( ( m_Flags & flag ) != 0 );
		}

		private void SetFlag( GainSkillObjectiveFlags flag, bool value )
		{
			if ( value )
				m_Flags |= flag;
			else
				m_Flags &= ~flag;
		}
	}

	// On OSI, once this is complete, it will *stay* complete, even if you lower your skill again
	public class GainSkillObjectiveInstance : BaseObjectiveInstance
	{
		private GainSkillObjective m_Objective;

		public GainSkillObjective Objective
		{
			get { return m_Objective; }
			set { m_Objective = value; }
		}

		public GainSkillObjectiveInstance( GainSkillObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			m_Objective = objective;
		}

		public bool Handles( SkillName skill )
		{
			return ( m_Objective.Skill == skill );
		}

		public override bool IsCompleted()
		{
			PlayerMobile pm = Instance.Player;

			int valueFixed = m_Objective.UseReal ? pm.Skills[m_Objective.Skill].Fixed : pm.Skills[m_Objective.Skill].BaseFixedPoint;

			return ( valueFixed >= m_Objective.ThresholdFixed );
		}

		// TODO: This may interfere with scrolls, or even quests among each other
		// How does OSI deal with this?
		public override void OnQuestAccepted()
		{
			if ( !m_Objective.Accelerate )
				return;

			PlayerMobile pm = Instance.Player;

			pm.AcceleratedSkill = m_Objective.Skill;
			pm.AcceleratedStart = DateTime.Now + TimeSpan.FromMinutes( 15 ); // TODO: Is there a max duration?
		}

		public override void OnQuestCancelled()
		{
			if ( !m_Objective.Accelerate )
				return;

			PlayerMobile pm = Instance.Player;

			pm.AcceleratedStart = DateTime.Now;
			pm.PlaySound( 0x100 );
		}

		public override void OnQuestCompleted()
		{
			OnQuestCancelled();
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			m_Objective.WriteToGump( g, ref y );

			base.WriteToGump( g, ref y );

			if ( IsCompleted() )
			{
				g.AddHtmlLocalized( 113, y, 312, 20, 1055121, 0xFFFFFF, false, false ); // Complete
				y += 16;
			}
		}
	}
}
