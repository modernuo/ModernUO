using System;

namespace Server.Factions
{
	public interface IFactionItem
	{
		FactionItem FactionItemState{ get; set; }
	}

	public class FactionItem
	{
		public static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays( 21.0 );

		public Item Item { get; }

		public Faction Faction { get; }

		public DateTime Expiration { get; private set; }

		public bool HasExpired
		{
			get
			{
				if ( Item == null || Item.Deleted )
					return true;

				return ( Expiration != DateTime.MinValue && DateTime.UtcNow >= Expiration );
			}
		}

		public void StartExpiration()
		{
			Expiration = DateTime.UtcNow + ExpirationPeriod;
		}

		public void CheckAttach()
		{
			if ( !HasExpired )
				Attach();
			else
				Detach();
		}

		public void Attach()
		{
			if ( Item is IFactionItem item )
				item.FactionItemState = this;

			Faction?.State.FactionItems.Add( this );
		}

		public void Detach()
		{
			if ( Item is IFactionItem item )
				item.FactionItemState = null;

			if ( Faction != null && Faction.State.FactionItems.Contains( this ) )
				Faction.State.FactionItems.Remove( this );
		}

		public FactionItem( Item item, Faction faction )
		{
			Item = item;
			Faction = faction;
		}

		public FactionItem( GenericReader reader, Faction faction )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 0:
				{
					Item = reader.ReadItem();
					Expiration = reader.ReadDateTime();
					break;
				}
			}

			Faction = faction;
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 );

			writer.Write( (Item) Item );
			writer.Write( (DateTime) Expiration );
		}

		public static int GetMaxWearables( Mobile mob )
		{
			PlayerState pl = PlayerState.Find( mob );

			if ( pl == null )
				return 0;

			if ( pl.Faction.IsCommander( mob ) )
				return 9;

			return pl.Rank.MaxWearables;
		}

		public static FactionItem Find( Item item )
		{
			if ( item is IFactionItem factionItem )
			{
				FactionItem state = factionItem.FactionItemState;

				if ( state?.HasExpired == true )
				{
					state.Detach();
					state = null;
				}

				return state;
			}

			return null;
		}

		public static Item Imbue( Item item, Faction faction, bool expire, int hue )
		{
			if ( !(item is IFactionItem) )
				return item;

			FactionItem state = Find( item );

			if ( state == null )
			{
				state = new FactionItem( item, faction );
				state.Attach();
			}

			if ( expire )
				state.StartExpiration();

			item.Hue = hue;
			return item;
		}
	}
}
