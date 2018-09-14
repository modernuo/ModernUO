using System;
using Server.Mobiles;
using Server.Gumps;

namespace Server.Engines.MLQuests.Objectives
{
	public class KillObjective : BaseObjective
	{
		public int DesiredAmount { get; set; }

		public Type[] AcceptedTypes { get; set; }

		public TextDefinition Name { get; set; }

		public QuestArea Area { get; set; }

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
			DesiredAmount = amount;
			AcceptedTypes = types;
			Name = name;
			Area = area;
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			string amount = DesiredAmount.ToString();

			g.AddHtmlLocalized( 98, y, 312, 16, 1072204, 0x15F90, false, false ); // Slay
			g.AddLabel( 133, y, 0x481, amount );

			if ( Name.Number > 0 )
				g.AddHtmlLocalized( 133 + amount.Length * 15, y, 190, 18, Name.Number, 0x77BF, false, false );
			else if ( Name.String != null )
				g.AddLabel( 133 + amount.Length * 15, y, 0x481, Name.String );

			y += 16;

			#region Location
			if ( Area != null )
			{
				g.AddHtmlLocalized( 103, y, 312, 20, 1018327, 0x15F90, false, false ); // Location

				if ( Area.Name.Number > 0 )
					g.AddHtmlLocalized( 223, y, 312, 20, Area.Name.Number, 0xFFFFFF, false, false );
				else if ( Area.Name.String != null )
					g.AddLabel( 223, y, 0x481, Area.Name.String );

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
		public override bool IsTimed => true;
		public override TimeSpan Duration { get; }

		public TimedKillObjective( TimeSpan duration, int amount, Type[] types, TextDefinition name )
			: this( duration, amount, types, name, null )
		{
		}

		public TimedKillObjective( TimeSpan duration, int amount, Type[] types, TextDefinition name, QuestArea area )
			: base( amount, types, name, area )
		{
			Duration = duration;
		}
	}

	#endregion

	public class KillObjectiveInstance : BaseObjectiveInstance
	{
		public KillObjective Objective { get; set; }

		public int Slain { get; set; }

		public KillObjectiveInstance( KillObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			Objective = objective;
			Slain = 0;
		}

		public bool AddKill( Mobile mob, Type type )
		{
			int desired = Objective.DesiredAmount;

			foreach ( Type acceptedType in Objective.AcceptedTypes )
			{
				if ( acceptedType.IsAssignableFrom( type ) )
				{
					if ( Objective.Area != null && !Objective.Area.Contains( mob ) )
						return false;

					PlayerMobile pm = Instance.Player;

					if ( ++Slain >= desired )
						pm.SendLocalizedMessage( 1075050 ); // You have killed all the required quest creatures of this type.
					else
						pm.SendLocalizedMessage( 1075051, ( desired - Slain ).ToString() ); // You have killed a quest creature. ~1_val~ more left.

					return true;
				}
			}

			return false;
		}

		public override bool IsCompleted()
		{
			return ( Slain >= Objective.DesiredAmount );
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			Objective.WriteToGump( g, ref y );

			base.WriteToGump( g, ref y );

			g.AddHtmlLocalized( 103, y, 120, 16, 3000087, 0x15F90, false, false ); // Total
			g.AddLabel( 223, y, 0x481, Slain.ToString() );
			y += 16;

			g.AddHtmlLocalized( 103, y, 120, 16, 1074782, 0x15F90, false, false ); // Return to
			g.AddLabel( 223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor( Instance.QuesterType ) );
			y += 16;
		}

		public override DataType ExtraDataType => DataType.KillObjective;

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( Slain );
		}
	}
}
