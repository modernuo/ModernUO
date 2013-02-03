using System;
using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Objectives
{
	public class DeliverObjective : BaseObjective
	{
		private Type m_Delivery;
		private int m_Amount;
		private TextDefinition m_Name;
		private Type m_Destination;
		private TextDefinition m_DestinationName;

		public Type Delivery
		{
			get { return m_Delivery; }
			set { m_Delivery = value; }
		}

		public int Amount
		{
			get { return m_Amount; }
			set { m_Amount = value; }
		}

		public TextDefinition Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public Type Destination
		{
			get { return m_Destination; }
			set { m_Destination = value; }
		}

		public TextDefinition DestinationName
		{
			get { return m_DestinationName; }
			set { m_DestinationName = value; }
		}

		public DeliverObjective( Type delivery, int amount, TextDefinition name, Type destination, TextDefinition destinationName )
		{
			m_Delivery = delivery;
			m_Amount = amount;
			m_Name = name;
			m_Destination = destination;
			m_DestinationName = destinationName;
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			string amount = m_Amount.ToString();

			g.AddHtmlLocalized( 98, y, 312, 16, 1072207, 0x15F90, false, false ); // Deliver
			g.AddLabel( 143, y, 0x481, amount );

			if ( m_Name.Number > 0 )
			{
				g.AddHtmlLocalized( 143 + amount.Length * 15, y, 190, 18, m_Name.Number, 0x77BF, false, false );
				g.AddItem( 350, y, CollectObjective.LabelToItemID( m_Name.Number ) );
			}
			else if ( m_Name.String != null )
			{
				g.AddLabel( 143 + amount.Length * 15, y, 0x481, m_Name.String );
			}

			y += 32;

			g.AddHtmlLocalized( 103, y, 120, 16, 1072379, 0x15F90, false, false ); // Deliver to

			if ( m_DestinationName.Number > 0 )
				g.AddHtmlLocalized( 223, y, 190, 18, m_DestinationName.Number, false, false );
			else
				g.AddLabel( 223, y, 0x481, m_DestinationName.String );

			y += 16;
		}

		public override BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			return new DeliverObjectiveInstance( this, instance );
		}
	}

	#region Timed

	public class TimedDeliverObjective : DeliverObjective
	{
		private TimeSpan m_Duration;

		public override bool IsTimed { get { return true; } }
		public override TimeSpan Duration { get { return m_Duration; } }

		public TimedDeliverObjective( TimeSpan duration, Type delivery, int amount, TextDefinition name, Type destination, TextDefinition destinationName )
			: base( delivery, amount, name, destination, destinationName )
		{
			m_Duration = duration;
		}
	}

	#endregion

	public class DeliverObjectiveInstance : BaseObjectiveInstance
	{
		private DeliverObjective m_Objective;
		private bool m_HasCompleted;

		public DeliverObjective Objective
		{
			get { return m_Objective; }
			set { m_Objective = value; }
		}

		public bool HasCompleted
		{
			get { return m_HasCompleted; }
			set { m_HasCompleted = value; }
		}

		public DeliverObjectiveInstance( DeliverObjective objective, MLQuestInstance instance )
			: base( instance, objective )
		{
			m_Objective = objective;
		}

		public virtual bool IsDestination( BaseCreature quester, Type type )
		{
			Type destType = m_Objective.Destination;

			return ( destType != null && destType.IsAssignableFrom( type ) );
		}

		public override bool IsCompleted()
		{
			return m_HasCompleted;
		}

		public override void OnQuestAccepted()
		{
			Container pack = Instance.Player.Backpack;

			if ( pack == null )
				return; // where are we supposed to put them?

			Type type = m_Objective.Delivery;
			int amount = m_Objective.Amount;

			List<Item> delivery = new List<Item>();

			for ( int i = 0; i < amount; ++i )
			{
				Item item = Activator.CreateInstance( type ) as Item;

				if ( item == null )
					continue;

				delivery.Add( item );

				if ( item.Stackable && amount > 1 )
				{
					item.Amount = amount;
					break;
				}
			}

			foreach ( Item item in delivery )
				pack.DropItem( item ); // Confirmed: on OSI items are added even if your pack is full
		}

		// This is VERY similar to CollectObjective.GetCurrentTotal
		private int GetCurrentTotal()
		{
			Container pack = Instance.Player.Backpack;

			if ( pack == null )
				return 0;

			Item[] items = pack.FindItemsByType( m_Objective.Delivery, false ); // Note: subclasses are included
			int total = 0;

			foreach ( Item item in items )
				total += item.Amount;

			return total;
		}

		public override bool OnBeforeClaimReward()
		{
			PlayerMobile pm = Instance.Player;

			int total = GetCurrentTotal();
			int desired = m_Objective.Amount;

			if ( total < desired )
			{
				pm.SendLocalizedMessage( 1074861 ); // You do not have everything you need!
				pm.SendLocalizedMessage( 1074885, String.Format( "{0}\t{1}", total, desired ) ); // You have ~1_val~ item(s) but require ~2_val~
				return false;
			}

			return true;
		}

		// TODO: This is VERY similar to CollectObjective.OnClaimReward
		public override void OnClaimReward()
		{
			Container pack = Instance.Player.Backpack;

			if ( pack == null )
				return;

			Item[] items = pack.FindItemsByType( m_Objective.Delivery, false );
			int left = m_Objective.Amount;

			foreach ( Item item in items )
			{
				if ( left == 0 )
					break;

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

		public override void OnQuestCancelled()
		{
			OnClaimReward(); // same effect
		}

		public override void OnExpire()
		{
			OnQuestCancelled();

			Instance.Player.SendLocalizedMessage( 1074813 ); // You have failed to complete your delivery.
		}

		public override void WriteToGump( Gump g, ref int y )
		{
			m_Objective.WriteToGump( g, ref y );

			base.WriteToGump( g, ref y );

			// No extra instance stuff printed for this objective
		}

		public override DataType ExtraDataType { get { return DataType.DeliverObjective; } }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( m_HasCompleted );
		}
	}
}
