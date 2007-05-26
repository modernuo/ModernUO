using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Items;

namespace Server.Misc
{
	public enum GiftResult
	{
		Backpack,
		BankBox
	}

	public class GiftGiving
	{
		private static List<GiftGiver> m_Givers = new List<GiftGiver>();

		public static void Register( GiftGiver giver )
		{
			m_Givers.Add( giver );
		}

		public static void Initialize()
		{
			EventSink.Login += new LoginEventHandler( EventSink_Login );
		}

		private static void EventSink_Login( LoginEventArgs e )
		{
			Account acct = e.Mobile.Account as Account;

			if ( acct == null )
				return;

			DateTime now = DateTime.Now;

			for ( int i = 0; i < m_Givers.Count; ++i )
			{
				GiftGiver giver = m_Givers[i];

				if ( now < giver.Start || now >= giver.Finish )
					continue; // not in the correct timefream

				if ( acct.Created > (giver.Start - giver.MinimumAge) )
					continue; // newly created account

				if ( acct.LastLogin >= giver.Start )
					continue; // already got one

				giver.DelayGiveGift( TimeSpan.FromSeconds( 5.0 ), e.Mobile );
			}

			acct.LastLogin = now;
		}
	}

	public abstract class GiftGiver
	{
		public virtual TimeSpan MinimumAge{ get{ return TimeSpan.FromDays( 30.0 ); } }

		public abstract DateTime Start{ get; }
		public abstract DateTime Finish{ get; }
		public abstract void GiveGift( Mobile mob );

		public virtual void DelayGiveGift( TimeSpan delay, Mobile mob )
		{
			Timer.DelayCall( delay, new TimerStateCallback( DelayGiveGift_Callback ), mob );
		}

		protected virtual void DelayGiveGift_Callback( object state )
		{
			GiveGift( (Mobile) state );
		}

		public virtual GiftResult GiveGift( Mobile mob, Item item )
		{
			if ( mob.PlaceInBackpack( item ) )
			{
				if ( !WeightOverloading.IsOverloaded( mob ) )
					return GiftResult.Backpack;
			}

			mob.BankBox.DropItem( item );
			return GiftResult.BankBox;
		}
	}
}