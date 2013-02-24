using System;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Engines.MLQuests.Objectives
{
	public class KillObjective : BaseObjective
	{
		private int m_DesiredAmount;
		private Type[] m_AcceptedTypes; // Example of Type[] requirement on OSI: killing X bone magis or skeletal mages (probably the same type on OSI though?)
		private TextDefinition m_Name;
		private QuestArea m_Area;

		public int DesiredAmount
		{
			get { return m_DesiredAmount; }
			set { m_DesiredAmount = value; }
		}

		public Type[] AcceptedTypes
		{
			get { return m_AcceptedTypes; }
			set { m_AcceptedTypes = value; }
		}

		public TextDefinition Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public QuestArea Area
		{
			get { return m_Area; }
			set { m_Area = value; }
		}

		public KillObjective()
			: this( 0, null, null, null )
		{
		}

		public KillObjective( int amount, Type[] types, TextDefinition name )
			: this( amount, types, name, null )
		{
		}

		public KillObjective( int amount, Type[] types, TextDefinition name, QuestArea area )
		{
			m_DesiredAmount = amount;
			m_AcceptedTypes = types;
			m_Name = name;
			m_Area = area;
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			string amount = m_DesiredAmount.ToString();

			g.AddHtmlLocalized( 98, y, 312, 16, 1072204, 0x15F90, false, false ); // Slay
			g.AddLabel( 133, y, 0x481, amount );

			if ( m_Name.Number > 0 )
				g.AddHtmlLocalized( 133 + amount.Length * 15, y, 190, 18, m_Name.Number, 0x77BF, false, false );
			else if ( m_Name.String != null )
				g.AddLabel( 133 + amount.Length * 15, y, 0x481, m_Name.String );

			y += 16;

			#region Location
			if ( m_Area != null )
			{
				g.AddHtmlLocalized( 103, y, 312, 20, 1018327, 0x15F90, false, false ); // Location

				if ( m_Area.Name.Number > 0 )
					g.AddHtmlLocalized( 223, y, 312, 20, m_Area.Name.Number, 0xFFFFFF, false, false );
				else if ( m_Area.Name.String != null )
					g.AddLabel( 223, y, 0x481, m_Area.Name.String );

				y += 16;
			}
			#endregion
		}

		public override BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			return new KillObjectiveInstance( this, instance );
		}
	}

	#region Timed

	public class TimedKillObjective : KillObjective
	{
		private TimeSpan m_Duration;

		public override bool IsTimed { get { return true; } }
		public override TimeSpan Duration { get { return m_Duration; } }

		public TimedKillObjective( TimeSpan duration, int amount, Type[] types, TextDefinition name )
			: this( duration, amount, types, name, null )
		{
		}

		public TimedKillObjective( TimeSpan duration, int amount, Type[] types, TextDefinition name, QuestArea area )
			: base( amount, types, name, area )
		{
			m_Duration = duration;
		}
	}

	#endregion

	public class KillObjectiveInstance : BaseObjectiveInstance
	{
		private KillObjective m_Objective;
		private int m_Slain;

		public KillObjective Objective
		{
			get { return m_Objective; }
			set { m_Objective = value; }
		}

		public int Slain
		{
			get { return m_Slain; }
			set { m_Slain = value; }
		}

		public KillObjectiveInstance( KillObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			m_Objective = objective;
			m_Slain = 0;
		}

		public bool AddKill( Mobile mob, Type type )
		{
			int desired = m_Objective.DesiredAmount;

			foreach ( Type acceptedType in m_Objective.AcceptedTypes )
			{
				if ( acceptedType.IsAssignableFrom( type ) )
				{
					if ( m_Objective.Area != null && !m_Objective.Area.Contains( mob ) )
						return false;

					PlayerMobile pm = Instance.Player;

					if ( ++m_Slain >= desired )
						pm.SendLocalizedMessage( 1075050 ); // You have killed all the required quest creatures of this type.
					else
						pm.SendLocalizedMessage( 1075051, ( desired - m_Slain ).ToString() ); // You have killed a quest creature. ~1_val~ more left.

					return true;
				}
			}

			return false;
		}

		public override bool IsCompleted()
		{
			return ( m_Slain >= m_Objective.DesiredAmount );
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			m_Objective.WriteToGump( g, ref y );

			base.WriteToGump( g, ref y );

			g.AddHtmlLocalized( 103, y, 120, 16, 3000087, 0x15F90, false, false ); // Total
			g.AddLabel( 223, y, 0x481, m_Slain.ToString() );
			y += 16;

			g.AddHtmlLocalized( 103, y, 120, 16, 1074782, 0x15F90, false, false ); // Return to
			g.AddLabel( 223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor( Instance.QuesterType ) );
			y += 16;
		}

		public override DataType ExtraDataType { get { return DataType.KillObjective; } }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( m_Slain );
		}
	}
}
