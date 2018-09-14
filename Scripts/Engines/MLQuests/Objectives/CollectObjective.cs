using System;
using Server.Mobiles;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.MLQuests.Objectives
{
	public class CollectObjective : BaseObjective
	{
		public int DesiredAmount { get; set; }

		public Type AcceptedType { get; set; }

		public TextDefinition Name { get; set; }

		public virtual bool ShowDetailed => true;

		public CollectObjective()
			: this( 0, null, null )
		{
		}

		public CollectObjective( int amount, Type type, TextDefinition name )
		{
			DesiredAmount = amount;
			AcceptedType = type;
			Name = name;

			if ( MLQuestSystem.Debug && ShowDetailed && name.Number > 0 )
			{
				int itemid = LabelToItemID( name.Number );

				if ( itemid <= 0 || itemid > 0x4000 )
					Console.WriteLine( "Warning: cliloc {0} is likely giving the wrong item ID", name.Number );
			}
		}

		public bool CheckType( Type type )
		{
			return ( AcceptedType != null && AcceptedType.IsAssignableFrom( type ) );
		}

		public virtual bool CheckItem( Item item )
		{
			return true;
		}

		public static int LabelToItemID( int label )
		{
			if ( label < 1078872 )
				return ( label - 1020000 );
			return ( label - 1078872 );
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			if ( ShowDetailed )
			{
				string amount = DesiredAmount.ToString();

				g.AddHtmlLocalized( 98, y, 350, 16, 1072205, 0x15F90, false, false ); // Obtain
				g.AddLabel( 143, y, 0x481, amount );

				if ( Name.Number > 0 )
				{
					g.AddHtmlLocalized( 143 + amount.Length * 15, y, 190, 18, Name.Number, 0x77BF, false, false );
					g.AddItem( 350, y, LabelToItemID( Name.Number ) );
				}
				else if ( Name.String != null )
				{
					g.AddLabel( 143 + amount.Length * 15, y, 0x481, Name.String );
				}
			}
			else
			{
				if ( Name.Number > 0 )
					g.AddHtmlLocalized( 98, y, 312, 32, Name.Number, 0x15F90, false, false );
				else if ( Name.String != null )
					g.AddLabel( 98, y, 0x481, Name.String );
			}

			y += 32;
		}

		public override BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			return new CollectObjectiveInstance( this, instance );
		}
	}

	#region Timed

	public class TimedCollectObjective : CollectObjective
	{
		public override bool IsTimed => true;
		public override TimeSpan Duration { get; }

		public TimedCollectObjective( TimeSpan duration, int amount, Type type, TextDefinition name )
			: base( amount, type, name )
		{
			Duration = duration;
		}
	}

	#endregion

	public class CollectObjectiveInstance : BaseObjectiveInstance
	{
		public CollectObjective Objective { get; set; }

		public CollectObjectiveInstance( CollectObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			Objective = objective;
		}

		private int GetCurrentTotal()
		{
			Container pack = Instance.Player.Backpack;

			if ( pack == null )
				return 0;

			Item[] items = pack.FindItemsByType( Objective.AcceptedType, false ); // Note: subclasses are included
			int total = 0;

			foreach ( Item item in items )
			{
				if ( item.QuestItem && Objective.CheckItem( item ) )
					total += item.Amount;
			}

			return total;
		}

		public override bool AllowsQuestItem( Item item, Type type )
		{
			return ( Objective.CheckType( type ) && Objective.CheckItem( item ) );
		}

		public override bool IsCompleted()
		{
			return ( GetCurrentTotal() >= Objective.DesiredAmount );
		}

		public override void OnQuestCancelled()
		{
			PlayerMobile pm = Instance.Player;
			Container pack = pm.Backpack;

			if ( pack == null )
				return;

			Type checkType = Objective.AcceptedType;
			Item[] items = pack.FindItemsByType( checkType, false );

			foreach ( Item item in items )
			{
				if ( item.QuestItem && !MLQuestSystem.CanMarkQuestItem( pm, item, checkType ) ) // does another quest still need this item? (OSI just unmarks everything)
					item.QuestItem = false;
			}
		}

		// Should only be called after IsComplete() is checked to be true
		public override void OnClaimReward()
		{
			Container pack = Instance.Player.Backpack;

			if ( pack == null )
				return;

			// TODO: OSI also counts the item in the cursor?

			Item[] items = pack.FindItemsByType( Objective.AcceptedType, false );
			int left = Objective.DesiredAmount;

			foreach ( Item item in items )
			{
				if ( item.QuestItem && Objective.CheckItem( item ) )
				{
					if ( left == 0 )
						return;

					if ( item.Amount > left )
					{
						item.Consume( left );
						left = 0;
					}
					else
					{
						item.Delete();
						left -= item.Amount;
					}
				}
			}
		}

		public override void OnAfterClaimReward()
		{
			OnQuestCancelled(); // same thing, clear other quest items
		}

		public override void OnExpire()
		{
			OnQuestCancelled();

			// No message
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			Objective.WriteToGump( g, ref y );
			y -= 16;

			if ( Objective.ShowDetailed )
			{
				base.WriteToGump( g, ref y );

				g.AddHtmlLocalized( 103, y, 120, 16, 3000087, 0x15F90, false, false ); // Total
				g.AddLabel( 223, y, 0x481, GetCurrentTotal().ToString() );
				y += 16;

				g.AddHtmlLocalized( 103, y, 120, 16, 1074782, 0x15F90, false, false ); // Return to
				g.AddLabel( 223, y, 0x481, QuesterNameAttribute.GetQuesterNameFor( Instance.QuesterType ) );
				y += 16;
			}
		}
	}
}
